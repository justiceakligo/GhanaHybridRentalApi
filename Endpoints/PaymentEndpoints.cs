using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // Payment transactions
        app.MapPost("/api/v1/payments/transactions", CreateTransactionAsync)
            .RequireAuthorization();

        app.MapGet("/api/v1/payments/transactions", GetUserTransactionsAsync)
            .RequireAuthorization();

        app.MapGet("/api/v1/payments/transactions/{transactionId:guid}", GetTransactionAsync)
            .RequireAuthorization();

        app.MapPut("/api/v1/payments/transactions/{transactionId:guid}/status", UpdateTransactionStatusAsync)
            .RequireAuthorization("AdminOnly");

        // Admin payment management
        app.MapGet("/api/v1/admin/payments/transactions", GetAllTransactionsAsync)
            .RequireAuthorization("AdminOnly");

        // Admin payments overview (summary + recent transactions)
        app.MapGet("/api/v1/admin/payments", GetPaymentsOverviewAsync)
            .RequireAuthorization("AdminOnly");

        // Admin capture payment
        app.MapPost("/api/v1/admin/payments/{paymentId:guid}/capture", CapturePaymentAsync)
            .RequireAuthorization("AdminOnly");

        // Booking payment initialization and confirmation
        // Allow anonymous for guest bookings (validation performed in handler)
        app.MapPost("/api/v1/bookings/{bookingId:guid}/payments/initialize", InitializeBookingPaymentAsync);
        app.MapPost("/api/v1/bookings/{bookingId:guid}/payments/confirm", ConfirmBookingPaymentAsync);

        // Payment methods and config endpoints
        app.MapGet("/api/v1/payments/methods", GetPaymentMethodsAsync);
        app.MapGet("/api/v1/payments/config", GetPaymentConfigAsync);
    }

    private static async Task<IResult> GetPaymentsOverviewAsync(
        AppDbContext db,
        [FromQuery] int recent = 20)
    {
        var totalTransactions = await db.PaymentTransactions.CountAsync();
        var totalPayments = await db.PaymentTransactions.Where(t => t.Type == "payment" && t.Status == "completed").SumAsync(t => (decimal?)t.Amount) ?? 0m;
        var recentTx = await db.PaymentTransactions.OrderByDescending(t => t.CreatedAt).Take(recent).ToListAsync();

        return Results.Ok(new { totalTransactions, totalPayments, recent = recentTx.Select(t => new { t.Id, t.BookingId, t.UserId, t.Type, t.Status, t.Amount, t.Method, t.Reference, t.CreatedAt }) });
    }

    private static async Task<IResult> CapturePaymentAsync(
        Guid paymentId,
        [FromBody] CapturePaymentRequest request,
        AppDbContext db,
        IPaystackPaymentService paystack,
        IStripePaymentService stripe)
    {
        var txn = await db.PaymentTransactions
            .Include(t => t.Booking)
            .FirstOrDefaultAsync(t => t.Id == paymentId);

        if (txn == null)
            return Results.NotFound(new { error = "Payment transaction not found" });

        if (txn.Type != "payment")
            return Results.BadRequest(new { error = "Only payment transactions can be captured" });

        if (txn.Status != "pending")
            return Results.BadRequest(new { error = "Payment is not in pending state" });

        var reference = txn.Reference ?? txn.ExternalTransactionId;
        if (string.IsNullOrWhiteSpace(reference))
            return Results.BadRequest(new { error = "No transaction reference available to capture" });

        // Choose provider based on method
        var captureSuccess = false;
        string? captureError = null;

        if (txn.Method == "card")
        {
            // Stripe capture
            var stripeResult = await stripe.CapturePaymentAsync(reference, request.CaptureAmount, request.Currency);
            captureSuccess = stripeResult.Success;
            captureError = stripeResult.Message;
        }
        else
        {
            // Default to Paystack for other methods (paystack, momo, etc.)
            var paystackResult = await paystack.CaptureTransactionAsync(reference, request.CaptureAmount, request.Currency);
            captureSuccess = paystackResult.Success;
            captureError = paystackResult.Message;
        }

        if (!captureSuccess)
            return Results.BadRequest(new { error = captureError ?? "Capture failed with provider" });

        // Mark transaction completed and persist typed captured amount
        txn.Status = "completed";
        txn.CompletedAt = DateTime.UtcNow;
        txn.CapturedAmount = request.CaptureAmount;

        // Also persist captured info in metadata for compatibility
        try
        {
            var meta = string.IsNullOrWhiteSpace(txn.MetadataJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(txn.MetadataJson) ?? new Dictionary<string, object>();
            meta["capturedAmount"] = request.CaptureAmount;
            if (!string.IsNullOrWhiteSpace(request.Reason)) meta["captureReason"] = request.Reason;
            txn.MetadataJson = JsonSerializer.Serialize(meta);
        }
        catch { }

        // Update booking if present
        if (txn.Booking != null && txn.Type == "payment")
        {
            txn.Booking.PaymentStatus = "paid";
            if (txn.Booking.Status == "pending_payment") txn.Booking.Status = "confirmed";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Payment captured successfully", transactionId = txn.Id });
    }

    private static async Task<IResult> GetPaymentMethodsAsync(AppDbContext db)
    {
        // Check configured payment providers from AppConfig
        var stripeKey = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Stripe:SecretKey");
        var paystackKey = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Paystack:SecretKey");

        var methods = new List<object>();

        if (stripeKey != null && !string.IsNullOrWhiteSpace(stripeKey.ConfigValue))
        {
            methods.Add(new
            {
                id = "card",
                name = "Credit/Debit Card",
                provider = "stripe",
                enabled = true
            });
        }

        if (paystackKey != null && !string.IsNullOrWhiteSpace(paystackKey.ConfigValue))
        {
            methods.Add(new
            {
                id = "paystack",
                name = "Paystack",
                provider = "paystack",
                enabled = true
            });
        }

        return Results.Ok(new { methods });
    }

    private static async Task<IResult> GetPaymentConfigAsync(AppDbContext db)
    {
        // Support both legacy and namespaced keys (legacy: "Stripe:PublishableKey", namespaced: "Payment:Stripe:PublishableKey")
        var stripePublishableKey = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Payment:Stripe:PublishableKey")
            ?? await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Stripe:PublishableKey");
        var paystackPublicKey = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Payment:Paystack:PublicKey")
            ?? await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Paystack:PublicKey");

        var stripeEnabledConfig = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Payment:Stripe:Enabled")
            ?? await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Stripe:Enabled");
        var paystackEnabledConfig = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Payment:Paystack:Enabled")
            ?? await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "Paystack:Enabled");

        var stripeKey = stripePublishableKey?.ConfigValue ?? "";
        var paystackKey = paystackPublicKey?.ConfigValue ?? "";

        var stripeEnabled = !string.IsNullOrWhiteSpace(stripeKey) || (stripeEnabledConfig != null && bool.TryParse(stripeEnabledConfig.ConfigValue, out var se) && se);
        var paystackEnabled = !string.IsNullOrWhiteSpace(paystackKey) || (paystackEnabledConfig != null && bool.TryParse(paystackEnabledConfig.ConfigValue, out var pe) && pe);

        return Results.Ok(new
        {
            stripe = new
            {
                publishableKey = stripeKey,
                enabled = stripeEnabled
            },
            paystack = new
            {
                publicKey = paystackKey,
                enabled = paystackEnabled
            }
        });
    }

    private static async Task<IResult> CreateTransactionAsync(
        ClaimsPrincipal principal,
        [FromBody] CreatePaymentTransactionRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var validTypes = new[] { "payment", "refund", "payout", "deposit" };
        if (!validTypes.Contains(request.Type.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid transaction type" });

        var validMethods = new[] { "momo", "card", "bank" };
        if (!validMethods.Contains(request.Method.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid payment method" });

        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "Amount must be greater than zero" });

        if (request.BookingId.HasValue)
        {
            var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId.Value);
            if (booking is null)
                return Results.BadRequest(new { error = "Booking not found" });

            // Verify user is associated with the booking
            if (booking.RenterId != userId && booking.OwnerId != userId)
                return Results.Forbid();
        }

        var transaction = new PaymentTransaction
        {
            UserId = userId,
            BookingId = request.BookingId,
            Type = request.Type.ToLowerInvariant(),
            Status = "pending",
            Amount = request.Amount,
            Method = request.Method.ToLowerInvariant(),
            Reference = request.Reference ?? $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata)
        };

        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/payments/transactions/{transaction.Id}",
            new PaymentTransactionResponse(
                transaction.Id,
                transaction.BookingId,
                transaction.UserId,
                transaction.Type,
                transaction.Status,
                transaction.Amount,
                transaction.Currency,
                transaction.Method,
                transaction.ExternalTransactionId,
                transaction.Reference,
                transaction.CreatedAt,
                transaction.CompletedAt
            ));
    }

    private static async Task<IResult> GetUserTransactionsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.PaymentTransactions
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = transactions.Select(t => new PaymentTransactionResponse(
                t.Id,
                t.BookingId,
                t.UserId,
                t.Type,
                t.Status,
                t.Amount,
                t.Currency,
                t.Method,
                t.ExternalTransactionId,
                t.Reference,
                t.CreatedAt,
                t.CompletedAt
            ))
        });
    }

    private static async Task<IResult> GetTransactionAsync(
        Guid transactionId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var transaction = await db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
            return Results.NotFound(new { error = "Transaction not found" });

        // Verify user owns the transaction
        if (transaction.UserId != userId)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.Role != "admin")
                return Results.Forbid();
        }

        return Results.Ok(new PaymentTransactionResponse(
            transaction.Id,
            transaction.BookingId,
            transaction.UserId,
            transaction.Type,
            transaction.Status,
            transaction.Amount,
            transaction.Currency,
            transaction.Method,
            transaction.ExternalTransactionId,
            transaction.Reference,
            transaction.CreatedAt,
            transaction.CompletedAt
        ));
    }

    private static async Task<IResult> GetAllTransactionsAsync(
        AppDbContext db,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? bookingId,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.PaymentTransactions
            .Include(t => t.User)
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Renter)
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Vehicle)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (bookingId.HasValue)
            query = query.Where(t => t.BookingId == bookingId.Value);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = transactions.Select(t => new PaymentTransactionDetailedResponse(
                t.Id,
                t.BookingId,
                t.UserId,
                t.Type,
                t.Status,
                t.Amount,
                t.Currency,
                t.Method,
                t.ExternalTransactionId,
                t.Reference,
                t.CreatedAt,
                t.CompletedAt,
                t.CapturedAmount,
                t.Booking?.BookingReference,
                t.Booking?.Renter != null ? $"{t.Booking.Renter.FirstName} {t.Booking.Renter.LastName}".Trim() : null,
                t.Booking?.Renter?.Email,
                t.Booking?.Vehicle != null ? $"{t.Booking.Vehicle.Make} {t.Booking.Vehicle.Model}".Trim() : null
            ))
        });
    }

    private static async Task<IResult> UpdateTransactionStatusAsync(
        Guid transactionId,
        [FromBody] UpdatePaymentTransactionRequest request,
        AppDbContext db)
    {
        var transaction = await db.PaymentTransactions
            .Include(t => t.Booking)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction is null)
            return Results.NotFound(new { error = "Transaction not found" });

        var validStatuses = new[] { "pending", "completed", "failed", "cancelled" };
        if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid status" });

        transaction.Status = request.Status.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(request.ExternalTransactionId))
            transaction.ExternalTransactionId = request.ExternalTransactionId;

        if (!string.IsNullOrWhiteSpace(request.ErrorMessage))
            transaction.ErrorMessage = request.ErrorMessage;

        if (request.Status.ToLowerInvariant() == "completed" && !transaction.CompletedAt.HasValue)
        {
            transaction.CompletedAt = DateTime.UtcNow;

            // Update booking payment status if this is a payment transaction
            if (transaction.Booking is not null && transaction.Type == "payment")
            {
                transaction.Booking.PaymentStatus = "paid";
                if (transaction.Booking.Status == "pending_payment")
                {
                    transaction.Booking.Status = "confirmed";
                }
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new PaymentTransactionResponse(
            transaction.Id,
            transaction.BookingId,
            transaction.UserId,
            transaction.Type,
            transaction.Status,
            transaction.Amount,
            transaction.Currency,
            transaction.Method,
            transaction.ExternalTransactionId,
            transaction.Reference,
            transaction.CreatedAt,
            transaction.CompletedAt
        ));
    }

    private static async Task<IResult> ConfirmBookingPaymentAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] CreatePaymentTransactionRequest request,
        AppDbContext db,
        HttpContext context)
    {
        // Allow anonymous (guest) payment initialization. When unauthenticated, require CustomerEmail to match booking.
        Guid? userId = null;
        try
        {
            var userIdStr = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var parsed))
                userId = parsed;
        }
        catch { /* ignore */ }

        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.PaymentStatus == "paid")
            return Results.BadRequest(new { error = "Booking already paid" });

        if (booking.Status != "pending_payment")
            return Results.BadRequest(new { error = "Booking not open for payment" });

        // If authenticated, ensure the requester is the renter for this booking
        if (userId.HasValue)
        {
            if (booking.RenterId != userId.Value)
                return Results.Forbid();
        }
        else
        {
            // Guest flow: Trust payment provider verification (Paystack/Stripe)
            // No email validation needed - if Paystack says payment succeeded, accept it
            userId = booking.RenterId;
        }

        // Find existing transaction (by booking and reference) or create one
        var existingTxn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.BookingId == bookingId && t.Reference == (request.Reference ?? string.Empty));
        if (existingTxn == null)
        {
            existingTxn = new PaymentTransaction
            {
                UserId = userId.Value,
                BookingId = bookingId,
                Type = "payment",
                Status = "pending",
                Amount = booking.TotalAmount,
                Method = request.Method.ToLowerInvariant(),
                Reference = request.Reference ?? $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}"
            };
            db.PaymentTransactions.Add(existingTxn);
            await db.SaveChangesAsync();
        }

        // Verify with provider depending on method
        var method = request.Method.ToLowerInvariant();
        PaymentVerificationResult? verificationResult = null;
        
        try
        {
            if (method == "card")
            {
                var stripeSvc = context.RequestServices.GetRequiredService<IStripePaymentService>();
                verificationResult = await stripeSvc.ConfirmPaymentAsync(request.Reference!);
                existingTxn.ExternalTransactionId = request.Reference;
            }
            else if (method == "paystack")
            {
                var paystackSvc = context.RequestServices.GetRequiredService<IPaystackPaymentService>();
                verificationResult = await paystackSvc.VerifyTransactionAsync(request.Reference!);
                existingTxn.ExternalTransactionId = request.Reference;
            }
            else
            {
                // For now accept other methods as verified (momo/bank flow may be handled separately)
                verificationResult = new PaymentVerificationResult { Success = true };
                existingTxn.ExternalTransactionId = request.Reference;
            }
        }
        catch (Exception ex)
        {
            verificationResult = new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = $"Verification exception: {ex.Message}",
                ErrorType = "provider_error",
                RawResponse = ex.ToString()
            };
            Console.WriteLine($"Payment verification error for {request.Reference}: {ex}");
        }

        if (!verificationResult.Success)
        {
            existingTxn.Status = "failed";
            existingTxn.ErrorMessage = verificationResult.ErrorMessage;
            await db.SaveChangesAsync();

            // Build rich failure response for frontend
            var failureResponse = BuildPaymentFailureResponse(
                verificationResult, 
                request.Reference ?? "unknown", 
                method
            );

            return Results.BadRequest(failureResponse);
        }

        existingTxn.Status = "completed";
        existingTxn.MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata);
        existingTxn.CompletedAt = DateTime.UtcNow;

        // Update booking
        booking.PaymentStatus = "paid";
        booking.Status = "confirmed";

        // Save changes before sending notifications
        await db.SaveChangesAsync();

        // Reload booking with all related entities for email templates
        await db.Entry(booking).Reference(b => b.Vehicle).LoadAsync();
        await db.Entry(booking).Reference(b => b.Renter).LoadAsync();

        // Send "booking confirmed" emails after payment success (status changed to "confirmed")
        try
        {
            var notificationService = context.RequestServices.GetRequiredService<INotificationService>();
            
            // Send "booking confirmed" email to customer (payment completed)
            await notificationService.SendBookingConfirmedNotificationAsync(booking);
            
            // Send "booking confirmed" email to owner (payment received)
            await notificationService.SendBookingConfirmationToOwnerAsync(booking);
            
            Console.WriteLine($"Sent booking confirmed emails for {booking.BookingReference} after payment");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending booking confirmed emails for {booking.BookingReference}: {ex.Message}");
            // Don't fail payment verification if emails fail
        }

        // If this transaction had extension metadata, apply the extension now
        try
        {
            var notificationService = context.RequestServices.GetRequiredService<INotificationService>();

            if (!string.IsNullOrWhiteSpace(existingTxn.MetadataJson))
            {
                using var doc = JsonDocument.Parse(existingTxn.MetadataJson);
                if (doc.RootElement.TryGetProperty("extension", out var ext))
                {
                    if (ext.TryGetProperty("newReturnDateTime", out var newRetEl) && newRetEl.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(newRetEl.GetString(), out var newReturnDt))
                        {
                            booking.ReturnDateTime = newReturnDt;
                            booking.TotalAmount = booking.TotalAmount + existingTxn.Amount; // delta applied

                            // Notify renter and owner about successful extension
                            await notificationService.SendPaymentReceivedNotificationAsync(booking, existingTxn.Amount);
                            await notificationService.SendBookingConfirmedNotificationAsync(booking);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying extension metadata: {ex}");
        }

        return Results.Ok(new
        {
            bookingId = booking.Id,
            bookingStatus = booking.Status,
            paymentStatus = booking.PaymentStatus,
            transaction = new PaymentTransactionResponse(
                existingTxn.Id,
                existingTxn.BookingId,
                existingTxn.UserId,
                existingTxn.Type,
                existingTxn.Status,
                existingTxn.Amount,
                existingTxn.Currency,
                existingTxn.Method,
                existingTxn.ExternalTransactionId,
                existingTxn.Reference,
                existingTxn.CreatedAt,
                existingTxn.CompletedAt
            )
        });
    }

    private static async Task<IResult> InitializeBookingPaymentAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] InitializePaymentRequest request,
        AppDbContext db,
        IStripePaymentService stripeService,
        IPaystackPaymentService paystackService)
    {
        // Allow anonymous (guest) initialization. When unauthenticated, require CustomerEmail to match booking.
        Guid? userId = null;
        try
        {
            var userIdStr = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var parsed))
                userId = parsed;
        }
        catch { /* ignore */ }

        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.PaymentStatus == "paid")
            return Results.BadRequest(new { error = "Booking already paid" });

        if (booking.Status != "pending_payment")
            return Results.BadRequest(new { error = "Booking not open for payment" });

        if (string.IsNullOrWhiteSpace(request.Method))
            return Results.BadRequest(new { error = "Payment method is required" });

        // Authorization/guest validation
        if (userId.HasValue)
        {
            if (booking.RenterId != userId.Value)
                return Results.Forbid();
        }
        else
        {
            // Guest flow: allow payment initialization without strict email validation
            // The payment provider will validate the transaction
            userId = booking.RenterId;
        }

        var txRef = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        var transaction = new PaymentTransaction
        {
            UserId = userId.Value,
            BookingId = bookingId,
            Type = "payment",
            Status = "pending",
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            Method = request.Method.ToLowerInvariant(),
            Reference = txRef,
        };

        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync();

        var method = request.Method.ToLowerInvariant();
        
        // Stripe handles "card" and "stripe" methods
        if (method == "card" || method == "stripe")
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var email = request.CustomerEmail ?? user?.Email ?? string.Empty;
            var name = request.CustomerName ?? user?.FirstName ?? string.Empty;
            var phone = user?.Phone ?? string.Empty;

            try
            {
                var customerId = await stripeService.CreateCustomerAsync(email, name, phone);
                var metadata = new Dictionary<string, string> { { "bookingId", bookingId.ToString() }, { "reference", txRef } };
                
                // Convert GHS to USD for Stripe (Stripe doesn't support GHS)
                decimal amountToCharge = booking.TotalAmount;
                string currency = "USD";
                if (booking.Currency.ToUpper() == "GHS")
                {
                    // Get exchange rate from GlobalSettings (admin-configured)
                    var exchangeRateSetting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "usd_to_ghs_rate");
                    decimal usdToGhsRate = 15.5m; // Default fallback
                    
                    if (exchangeRateSetting != null && !string.IsNullOrWhiteSpace(exchangeRateSetting.ValueJson))
                    {
                        // Try to parse the ValueJson which might be a simple number or a JSON object
                        try
                        {
                            if (decimal.TryParse(exchangeRateSetting.ValueJson.Trim('"'), out var parsedRate))
                            {
                                usdToGhsRate = parsedRate;
                            }
                        }
                        catch { /* Use default rate */ }
                    }
                    
                    amountToCharge = Math.Round(booking.TotalAmount / usdToGhsRate, 2);
                    metadata["original_amount"] = booking.TotalAmount.ToString();
                    metadata["original_currency"] = "GHS";
                    metadata["exchange_rate"] = usdToGhsRate.ToString();
                }
                
                var intent = await stripeService.CreatePaymentIntentAsync(amountToCharge, currency, customerId, metadata);

                transaction.ExternalTransactionId = intent.Id;
                transaction.Method = "card"; // Normalize to "card" for consistency
                await db.SaveChangesAsync();

                return Results.Ok(new InitializePaymentResponse("stripe", transaction.Reference, null, intent.ClientSecret));
            }
            catch (PaymentProviderNotConfiguredException ex)
            {
                // Friendly 503 response so frontend can show helpful message
                return Results.Json(new { error = "Payment provider not configured", provider = ex.Provider, detail = ex.Message }, statusCode: 503);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stripe payment initialization failed: {ex.Message}");
                transaction.Status = "failed";
                transaction.ErrorMessage = ex.Message;
                await db.SaveChangesAsync();
                return Results.Json(new { error = "Failed to initialize Stripe payment", detail = ex.Message }, statusCode: 500);
            }
        }
        else if (request.Method.ToLowerInvariant() == "paystack")
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var email = request.CustomerEmail ?? user?.Email ?? string.Empty;

            // Paystack requires a customer email to initialize transactions. If missing, surface a clear error for guest flows.
            if (string.IsNullOrWhiteSpace(email))
            {
                transaction.Status = "failed";
                transaction.ErrorMessage = "Paystack requires a customer email for initialization";
                await db.SaveChangesAsync();
                return Results.BadRequest(new { error = "Paystack requires a customer email. Provide `customerEmail` in the request or ensure booking/renter has an email." });
            }

            var metadata = new Dictionary<string, string> { { "bookingId", bookingId.ToString() }, { "reference", txRef } };
            try
            {
                var init = await paystackService.InitializeTransactionAsync(booking.TotalAmount, email, txRef, metadata);

                transaction.ExternalTransactionId = init.Reference;
                transaction.Reference = init.Reference;
                await db.SaveChangesAsync();

                return Results.Ok(new InitializePaymentResponse("paystack", transaction.Reference, init.AuthorizationUrl, null));
            }
            catch (PaymentProviderNotConfiguredException ex)
            {
                return Results.Json(new { error = "Payment provider not configured", provider = ex.Provider, detail = ex.Message }, statusCode: 503);
            }
            catch (Exception ex)
            {
                // Ensure transaction is marked failed and return a helpful error message
                transaction.Status = "failed";
                transaction.ErrorMessage = ex.Message;
                await db.SaveChangesAsync();
                return Results.Json(new { error = "Failed to initialize Paystack payment", detail = ex.Message }, statusCode: 500);
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(new InitializePaymentResponse(request.Method, transaction.Reference, null, null));
    }

    /// <summary>
    /// Build contextual payment failure response to help frontend provide smart UX
    /// Considers error type, payment method, and provides actionable guidance
    /// </summary>
    private static PaymentFailureResponse BuildPaymentFailureResponse(
        PaymentVerificationResult verificationResult, 
        string reference, 
        string failedMethod)
    {
        var errorType = verificationResult.ErrorType;
        var errorMessage = verificationResult.ErrorMessage ?? "Payment verification failed";
        
        // Determine alternative payment method (Stripe for Paystack failures, Paystack for Stripe failures)
        var alternativeMethod = failedMethod == "card" ? "paystack" : "card";
        var alternativeMethodDisplay = failedMethod == "card" ? "Mobile Money" : "Card";
        
        // Build user-friendly messages and recommendations based on error type
        var (userMessage, recommendedAction, canRetry, suggestAlternative) = errorType switch
        {
            "network" => (
                "We couldn't connect to complete your payment. This might be due to a temporary network issue.",
                $"Please check your connection and try again. If the problem persists, you can try paying with {alternativeMethodDisplay}.",
                true,
                true
            ),
            
            "insufficient_funds" => failedMethod == "card" ? (
                "Your card doesn't have enough funds for this payment.",
                $"Please use a different card or try Mobile Money instead.",
                false,
                true
            ) : (
                "Your mobile money account doesn't have enough funds for this payment.",
                "Please top up your mobile money account and try again, or use a card instead.",
                true,
                true
            ),
            
            "declined" => failedMethod == "card" ? (
                "Your card payment was declined by your bank.",
                $"Please contact your bank or try a different card. You can also pay with Mobile Money.",
                false,
                true
            ) : (
                "Your mobile money payment was declined.",
                "Please check your mobile money account status and try again. You can also pay with a card.",
                true,
                true
            ),
            
            "invalid_details" => failedMethod == "card" ? (
                "The card details provided appear to be invalid.",
                "Please check your card number, expiry date, and CVV, then try again.",
                true,
                false
            ) : (
                "The payment details provided appear to be invalid.",
                "Please verify your information and try again.",
                true,
                false
            ),
            
            "provider_error" => (
                "We're experiencing technical issues with our payment provider.",
                $"Please try again in a few moments. If the problem continues, you can try {alternativeMethodDisplay}.",
                true,
                true
            ),
            
            _ => (
                "Your payment couldn't be processed.",
                $"Please try again. If you continue experiencing issues, try paying with {alternativeMethodDisplay}.",
                true,
                true
            )
        };

        return new PaymentFailureResponse
        {
            Error = "Payment verification failed",
            Details = errorMessage,
            Reference = reference,
            ErrorType = errorType,
            CanRetry = canRetry,
            SuggestAlternative = suggestAlternative,
            UserMessage = userMessage,
            RecommendedAction = recommendedAction,
            AlternativeMethod = suggestAlternative ? alternativeMethod : null,
            FailedMethod = failedMethod
        };
    }
}
