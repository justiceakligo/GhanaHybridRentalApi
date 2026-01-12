using System.Text.Json;
using System.IO;
using Stripe;
using GhanaHybridRentalApi.Services;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        // Partner webhook endpoints
        app.MapPost("/api/v1/webhooks/booking-created", BookingCreatedWebhookAsync);

        app.MapPost("/api/v1/webhooks/booking-completed", BookingCompletedWebhookAsync);

        app.MapPost("/api/v1/webhooks/booking-cancelled", BookingCancelledWebhookAsync);

        // Partner API endpoints (authenticated via API key)
        app.MapPost("/api/v1/partner/bookings", CreatePartnerBookingAsync);

        app.MapGet("/api/v1/partner/vehicles", GetAvailableVehiclesForPartnerAsync);

        app.MapGet("/api/v1/partner/protection-plans", GetProtectionPlansForPartnerAsync);

        app.MapPost("/api/v1/partner/validate-promo", ValidatePromoCodeForPartnerAsync);

        // Payment provider webhook endpoints
        app.MapPost("/api/v1/webhooks/stripe", StripeWebhookAsync);
        
        // Paystack webhook handles both POST (webhook) and GET (redirect/verification)
        app.MapPost("/api/v1/webhooks/paystack", PaystackWebhookAsync);
        app.MapGet("/api/v1/webhooks/paystack", PaystackWebhookAsync);
    }

    private static async Task<IResult> BookingCreatedWebhookAsync(
        [FromBody] WebhookNotification notification,
        AppDbContext db,
        HttpContext context)
    {
        // Verify webhook authenticity
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        // Process the notification (e.g., send to partner's webhook URL)
        if (!string.IsNullOrWhiteSpace(partner.WebhookUrl))
        {
            // In production, use HttpClient to send webhook
            // For now, just log it
            Console.WriteLine($"Webhook to {partner.Name}: {notification.Event}");
        }

        return Results.Ok(new { message = "Webhook received" });
    }

    private static async Task<IResult> BookingCompletedWebhookAsync(
        [FromBody] WebhookNotification notification,
        AppDbContext db,
        HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        if (!string.IsNullOrWhiteSpace(partner.WebhookUrl))
        {
            Console.WriteLine($"Booking completed webhook to {partner.Name}");
        }

        return Results.Ok(new { message = "Webhook received" });
    }

    private static async Task<IResult> BookingCancelledWebhookAsync(
        [FromBody] WebhookNotification notification,
        AppDbContext db,
        HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        if (!string.IsNullOrWhiteSpace(partner.WebhookUrl))
        {
            Console.WriteLine($"Booking cancelled webhook to {partner.Name}");
        }

        return Results.Ok(new { message = "Webhook received" });
    }

    private static async Task<IResult> CreatePartnerBookingAsync(
        [FromBody] PartnerBookingRequest request,
        AppDbContext db,
        HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        // Validate vehicle availability
        var vehicle = await db.Vehicles
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.Status == "active");

        if (vehicle is null)
            return Results.BadRequest(new { error = "Vehicle not available" });

        // Check for conflicts
        var hasConflict = await db.Bookings
            .AnyAsync(b => b.VehicleId == request.VehicleId &&
                          b.Status != "cancelled" &&
                          b.Status != "completed" &&
                          b.PickupDateTime < request.ReturnDateTime &&
                          b.ReturnDateTime > request.PickupDateTime);

        if (hasConflict)
            return Results.BadRequest(new { error = "Vehicle not available for requested dates" });

        // Find or create renter user
        var renterUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.RenterEmail);
        if (renterUser is null)
        {
            renterUser = new User
            {
                Email = request.RenterEmail,
                Phone = request.RenterPhone,
                Role = "renter",
                Status = "active"
            };

            db.Users.Add(renterUser);
            await db.SaveChangesAsync();

            db.RenterProfiles.Add(new RenterProfile
            {
                UserId = renterUser.Id,
                FullName = request.RenterName,
                VerificationStatus = "basic_verified"
            });
            await db.SaveChangesAsync();
        }

        // Create referral if partner has referral code
        if (!string.IsNullOrWhiteSpace(partner.ReferralCode))
        {
            var existingReferral = await db.Referrals
                .FirstOrDefaultAsync(r => r.ReferredUserId == renterUser.Id);

            if (existingReferral is null)
            {
                db.Referrals.Add(new Referral
                {
                    ReferralCode = partner.ReferralCode,
                    ReferredUserId = renterUser.Id,
                    IntegrationPartnerId = partner.Id,
                    Status = "pending"
                });
            }
        }

        // Calculate pricing (same logic as direct bookings)
        var totalDays = (request.ReturnDateTime.Date - request.PickupDateTime.Date).TotalDays;
        if (totalDays < 1) totalDays = 1;

        // Ensure category exists
        if (vehicle.Category == null)
            return Results.BadRequest(new { error = "Vehicle category data missing" });

        var dailyRate = vehicle.DailyRate ?? vehicle.Category.DefaultDailyRate;
        var rentalAmount = dailyRate * (decimal)totalDays;
        var depositAmount = vehicle.Category.DefaultDepositAmount;

        // Driver amount (optional)
        decimal? driverAmount = null;
        if (request.WithDriver)
        {
            var driverDailyRate = await db.GlobalSettings
                .Where(s => s.Key == "DriverDailyRate")
                .Select(s => s.ValueJson)
                .FirstOrDefaultAsync();
            
            if (!string.IsNullOrWhiteSpace(driverDailyRate))
            {
                try 
                { 
                    var rate = JsonSerializer.Deserialize<decimal>(driverDailyRate);
                    driverAmount = rate * (decimal)totalDays;
                } 
                catch { driverAmount = 150m * (decimal)totalDays; } // Default 150 GHS/day
            }
            else
            {
                driverAmount = 150m * (decimal)totalDays; // Default 150 GHS/day
            }
        }

        // Protection plan handling (mandatory, same as direct bookings)
        ProtectionPlan? protectionPlan = null;
        decimal? protectionAmount = null;

        if (request.ProtectionPlanId.HasValue)
        {
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.Id == request.ProtectionPlanId.Value && p.IsActive);
        }
        else
        {
            // If not specified, use default protection plan
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.IsActive && p.IsDefault);
        }

        if (protectionPlan is null)
            return Results.BadRequest(new { error = "Protection plan is required. Please select a valid protection plan." });

        if (protectionPlan.PricingMode == "per_day")
        {
            protectionAmount = protectionPlan.DailyPrice * (decimal)totalDays;
        }
        else
        {
            protectionAmount = protectionPlan.FixedPrice;
        }

        protectionAmount = Math.Max(protectionPlan.MinFee, Math.Min(protectionAmount.Value, protectionPlan.MaxFee));

        // Get platform fee percentage from GlobalSettings (default 15%)
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }

        // Calculate platform fee (on rental amount + driver amount, same as direct bookings)
        var subtotal = rentalAmount + (driverAmount ?? 0m);
        var platformFee = subtotal * (platformFeePercentage / 100m);

        // Total amount includes all fees (same calculation as direct bookings)
        var totalAmount = rentalAmount + depositAmount + (driverAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;

        // Apply promo code if provided (same logic as direct bookings)
        Guid? promoCodeId = null;
        decimal promoDiscountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var promoCodeService = context.RequestServices.GetRequiredService<IPromoCodeService>();
            try
            {
                var categoryId = vehicle.CategoryId;
                var cityId = vehicle.CityId;
                
                var validationRequest = new Dtos.ValidatePromoCodeDto(
                    request.PromoCode,
                    totalAmount,
                    vehicle.Id,
                    categoryId,
                    cityId,
                    (int)totalDays
                );

                var validation = await promoCodeService.ValidatePromoCodeAsync(request.PromoCode, renterUser.Id, validationRequest);

                if (validation.IsValid && validation.PromoCode != null)
                {
                    promoDiscountAmount = validation.DiscountAmount;
                    promoCodeId = validation.PromoCode.Id;

                    // For owner vehicle discounts, reduce rental amount (owner earns less)
                    // For regular discounts, reduce total amount (renter pays less)
                    if (validation.PromoCode.PromoType == "OwnerVehicleDiscount")
                    {
                        rentalAmount -= promoDiscountAmount;
                        // Recalculate subtotal and fees
                        subtotal = rentalAmount + (driverAmount ?? 0m);
                        platformFee = subtotal * (platformFeePercentage / 100m);
                        totalAmount = rentalAmount + depositAmount + (driverAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;
                    }
                    else
                    {
                        // Regular promo - reduce total amount
                        totalAmount = validation.FinalAmount;
                    }
                }
                else
                {
                    // Return error if promo code is invalid
                    return Results.BadRequest(new { error = $"Promo code error: {validation.ErrorMessage}" });
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"Failed to apply promo code: {ex.Message}" });
            }
        }

        // Calculate partner commission and settlement (after promo code discount)
        var commissionPercent = partner.CommissionPercent;
        var commissionAmount = Math.Round(totalAmount * (commissionPercent / 100), 2);
        var settlementAmount = totalAmount - commissionAmount;

        // Generate unique booking reference
        var bookingReference = $"RV-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var booking = new Booking
        {
            BookingReference = bookingReference,
            RenterId = renterUser.Id,
            VehicleId = vehicle.Id,
            OwnerId = vehicle.OwnerId,
            PickupDateTime = request.PickupDateTime,
            ReturnDateTime = request.ReturnDateTime,
            WithDriver = request.WithDriver,
            DriverAmount = driverAmount,
            ProtectionPlanId = protectionPlan.Id,
            ProtectionAmount = protectionAmount,
            ProtectionSnapshotJson = JsonSerializer.Serialize(new 
            { 
                protectionPlan.Code, 
                protectionPlan.Name, 
                protectionPlan.DailyPrice, 
                protectionPlan.FixedPrice, 
                protectionPlan.MinFee, 
                protectionPlan.MaxFee, 
                protectionPlan.IncludesMinorDamageWaiver, 
                protectionPlan.MinorWaiverCap, 
                protectionPlan.Deductible, 
                protectionPlan.ExcludesJson 
            }),
            Currency = "GHS",
            RentalAmount = rentalAmount,
            DepositAmount = depositAmount,
            PlatformFee = platformFee,
            PromoCodeId = promoCodeId,
            PromoDiscountAmount = promoDiscountAmount,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod.ToLowerInvariant(),
            PaymentChannel = "partner", // Mark as partner booking
            Status = partner.AutoConfirmBookings ? "confirmed" : "pending", 
            PaymentStatus = "paid", // Customer paid partner
            IntegrationPartnerId = partner.Id,
            PartnerSettlementStatus = "pending" // Partner hasn't paid us yet
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(); // Save to get booking.Id

        // Create settlement record
        var settlementDueDate = DateTime.UtcNow.AddDays(partner.SettlementTermDays);
        var settlement = new PartnerSettlement
        {
            IntegrationPartnerId = partner.Id,
            BookingId = booking.Id,
            BookingReference = bookingReference,
            SettlementPeriodStart = booking.CreatedAt,
            SettlementPeriodEnd = booking.ReturnDateTime,
            TotalAmount = totalAmount,
            CommissionPercent = commissionPercent,
            CommissionAmount = commissionAmount,
            SettlementAmount = settlementAmount,
            Status = "pending",
            DueDate = settlementDueDate
        };

        db.PartnerSettlements.Add(settlement);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/bookings/{booking.Id}", new
        {
            booking.Id,
            bookingReference = booking.BookingReference,
            booking.RenterId,
            booking.VehicleId,
            booking.Status,
            booking.PaymentStatus,
            booking.TotalAmount,
            booking.PickupDateTime,
            booking.ReturnDateTime,
            partner = new
            {
                commissionPercent,
                commissionAmount,
                settlementAmount,
                settlementDueDate
            }
        });
    }

    private static async Task<IResult> GetProtectionPlansForPartnerAsync(
        AppDbContext db,
        HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        var protectionPlans = await db.ProtectionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DailyPrice)
            .ToListAsync();

        return Results.Ok(protectionPlans.Select(p => new
        {
            p.Id,
            p.Code,
            p.Name,
            p.Description,
            p.PricingMode,
            p.DailyPrice,
            p.FixedPrice,
            p.MinFee,
            p.MaxFee,
            p.Currency,
            p.IncludesMinorDamageWaiver,
            p.MinorWaiverCap,
            p.Deductible,
            exclusions = string.IsNullOrWhiteSpace(p.ExcludesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(p.ExcludesJson),
            p.IsMandatory,
            p.IsDefault
        }));
    }

    private static async Task<IResult> ValidatePromoCodeForPartnerAsync(
        [FromBody] PartnerPromoValidationRequest request,
        AppDbContext db,
        HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        // Get vehicle details for promo code validation
        var vehicle = await db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId);

        if (vehicle is null)
            return Results.BadRequest(new { error = "Vehicle not found" });

        var promoCodeService = context.RequestServices.GetRequiredService<IPromoCodeService>();

        try
        {
            var validationRequest = new Dtos.ValidatePromoCodeDto(
                request.PromoCode,
                request.BookingAmount,
                vehicle.Id,
                vehicle.CategoryId,
                vehicle.CityId,
                request.RentalDays
            );

            // Use partner user ID as a placeholder since customer doesn't exist yet
            var validation = await promoCodeService.ValidatePromoCodeAsync(
                request.PromoCode, 
                Guid.Empty, // Will be validated again during actual booking with real user ID
                validationRequest
            );

            if (validation.IsValid)
            {
                return Results.Ok(new
                {
                    isValid = true,
                    promoCode = validation.PromoCode?.Code,
                    promoType = validation.PromoCode?.PromoType,
                    discountType = validation.PromoCode?.PromoType,
                    discountAmount = validation.DiscountAmount,
                    originalAmount = validation.OriginalAmount,
                    finalAmount = validation.FinalAmount,
                    message = validation.ErrorMessage
                });
            }
            else
            {
                return Results.Ok(new
                {
                    isValid = false,
                    error = validation.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating promo code for partner: {ex.Message}");
            return Results.BadRequest(new { error = $"Failed to validate promo code: {ex.Message}" });
        }
    }

    private static async Task<IResult> GetAvailableVehiclesForPartnerAsync(
        AppDbContext db,
        HttpContext context,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? cityId) // Changed from string to Guid
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var (isValid, partner, errorMessage) = await PartnerAuthHelper.ValidatePartnerApiKeyAsync(apiKey, db);
        
        if (!isValid || partner is null)
            return Results.Json(new { error = errorMessage ?? "Unauthorized" }, statusCode: 401);

        var query = db.Vehicles
            .Include(v => v.Category)
            .Include(v => v.City)
            .Where(v => v.Status == "active");

        if (categoryId.HasValue)
            query = query.Where(v => v.CategoryId == categoryId.Value);

        if (cityId.HasValue)
            query = query.Where(v => v.CityId == cityId.Value);

        // Filter by availability if dates provided
        if (startDate.HasValue && endDate.HasValue)
        {
            var conflictingBookings = await db.Bookings
                .Where(b => b.Status != "cancelled" &&
                           b.Status != "completed" &&
                           b.PickupDateTime < endDate.Value &&
                           b.ReturnDateTime > startDate.Value)
                .Select(b => b.VehicleId)
                .ToListAsync();

            query = query.Where(v => !conflictingBookings.Contains(v.Id));
        }

        var vehicles = await query.ToListAsync();

        return Results.Ok(vehicles.Select(v => new
        {
            v.Id,
            v.Make,
            v.Model,
            v.Year,
            v.Transmission,
            v.FuelType,
            v.SeatingCapacity,
            v.HasAC,
            v.CityId,
            category = v.Category is null ? null : new
            {
                v.Category.Name,
                v.Category.DefaultDailyRate,
                v.Category.DefaultDepositAmount
            },
            photos = string.IsNullOrWhiteSpace(v.PhotosJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v.PhotosJson)
        }));
    }

    private static async Task<IResult> StripeWebhookAsync(HttpContext context, AppDbContext db, IAppConfigService configService)
    {
        var secret = await configService.GetConfigValueAsync("Payment:Stripe:WebhookSecret");
        if (string.IsNullOrWhiteSpace(secret))
            return Results.BadRequest(new { error = "Stripe webhook secret not configured" });

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var sigHeader = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
        try
        {
            var stripeEvent = Stripe.EventUtility.ConstructEvent(body, sigHeader, secret);
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = (Stripe.PaymentIntent)stripeEvent.Data.Object;
                var intentId = paymentIntent.Id;
                var metadataRef = paymentIntent.Metadata != null && paymentIntent.Metadata.ContainsKey("reference") ? paymentIntent.Metadata["reference"] : null;
                var txn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.ExternalTransactionId == intentId || (metadataRef != null && t.Reference == metadataRef));
                if (txn == null)
                {
                    txn = new PaymentTransaction
                    {
                        UserId = Guid.Empty,
                        BookingId = null,
                        Type = "payment",
                        Status = "completed",
                        Amount = (decimal)paymentIntent.AmountReceived / 100m,
                        Currency = paymentIntent.Currency?.ToUpperInvariant() ?? "GHS",
                        Method = "card",
                        ExternalTransactionId = intentId,
                        Reference = metadataRef ?? $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
                        CompletedAt = DateTime.UtcNow
                    };
                    db.PaymentTransactions.Add(txn);
                }
                else
                {
                    txn.Status = "completed";
                    txn.CompletedAt = DateTime.UtcNow;
                }

                if (txn.BookingId.HasValue)
                {
                    var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == txn.BookingId.Value);
                    if (booking is not null)
                    {
                        booking.PaymentStatus = "paid";
                        booking.Status = "confirmed";
                    }
                }

                await db.SaveChangesAsync();
            }

            return Results.Ok();
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"Stripe webhook verification failed: {ex.Message}");
            return Results.BadRequest(new { error = "Invalid stripe webhook signature" });
        }
    }

    private static async Task<IResult> PaystackWebhookAsync(HttpContext context, AppDbContext db, IAppConfigService configService)
    {
        // Check if this is a redirect from Paystack payment page (GET request with query params)
        if (context.Request.Method == "GET")
        {
            var trxref = context.Request.Query["trxref"].FirstOrDefault();
            var reference = context.Request.Query["reference"].FirstOrDefault();
            
            if (!string.IsNullOrWhiteSpace(reference))
            {
                // Find transaction to determine success/failure
                var txn = await db.PaymentTransactions
                    .Include(t => t.Booking)
                    .FirstOrDefaultAsync(t => t.Reference == reference || t.ExternalTransactionId == reference);
                
                var frontendUrl = await configService.GetConfigValueAsync("App:FrontendUrl") ?? "https://ryverental.com";
                
                if (txn != null && txn.Status == "completed" && txn.BookingId.HasValue)
                {
                    // Payment successful - redirect to booking confirmation
                    return Results.Redirect($"{frontendUrl}/booking-confirmation?bookingId={txn.BookingId}&success=true");
                }
                else if (txn != null && txn.BookingId.HasValue)
                {
                    // Payment pending or failed - redirect to retry page
                    return Results.Redirect($"{frontendUrl}/payment-retry?bookingId={txn.BookingId}&reference={reference}");
                }
                
                // No transaction found - redirect to home
                return Results.Redirect($"{frontendUrl}/?payment=unknown");
            }
            
            // No reference - just verify endpoint for Paystack setup
            return Results.Ok(new { message = "Paystack webhook endpoint verified" });
        }

        // POST request - actual webhook from Paystack
        var secret = await configService.GetConfigValueAsync("Payment:Paystack:WebhookSecret");
        if (string.IsNullOrWhiteSpace(secret))
            return Results.BadRequest(new { error = "Paystack webhook secret not configured" });

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var receivedSignature = context.Request.Headers["X-Paystack-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(receivedSignature))
            return Results.BadRequest(new { error = "Missing signature" });

        using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body));
        var computed = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        if (computed != receivedSignature.ToLowerInvariant())
            return Results.BadRequest(new { error = "Invalid signature" });

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var @event = json.GetProperty("event").GetString();
        var data = json.GetProperty("data");
        var status = data.GetProperty("status").GetString();
        var paymentReference = data.GetProperty("reference").GetString();

        if (@event == "charge.success" && status == "success")
        {
            var txn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.Reference == paymentReference || t.ExternalTransactionId == paymentReference);
            if (txn == null)
            {
                txn = new PaymentTransaction
                {
                    UserId = Guid.Empty,
                    BookingId = null,
                    Type = "payment",
                    Status = "completed",
                    Amount = data.GetProperty("amount").GetDecimal() / 100m,
                    Currency = data.GetProperty("currency").GetString() ?? "GHS",
                    Method = "paystack",
                    ExternalTransactionId = paymentReference,
                    Reference = paymentReference,
                    CompletedAt = DateTime.UtcNow
                };
                db.PaymentTransactions.Add(txn);
            }
            else
            {
                txn.Status = "completed";
                txn.CompletedAt = DateTime.UtcNow;
            }

            if (txn.BookingId.HasValue)
            {
                var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == txn.BookingId.Value);
                if (booking is not null)
                {
                    booking.PaymentStatus = "paid";
                    booking.Status = "confirmed";
                }
            }

            await db.SaveChangesAsync();
        }

        // Handle refund events
        if ((@event == "refund.success" || @event == "refund.failed") )
        {
            var refundId = data.GetProperty("id").GetString();
            string? txnRef = null;
            try
            {
                if (data.TryGetProperty("transaction", out var ttx) && ttx.TryGetProperty("reference", out var rr))
                    txnRef = rr.GetString();
            }
            catch { }

            // Try to find DepositRefund by ExternalRefundId or by related payment transaction
            DepositRefund? refund = null;
            if (!string.IsNullOrWhiteSpace(refundId))
            {
                refund = await db.DepositRefunds.FirstOrDefaultAsync(r => r.ExternalRefundId == refundId);
            }

            if (refund == null && !string.IsNullOrWhiteSpace(txnRef))
            {
                var payTxn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.Reference == txnRef || t.ExternalTransactionId == txnRef);
                if (payTxn != null)
                {
                    refund = await db.DepositRefunds.FirstOrDefaultAsync(r => r.BookingId == payTxn.BookingId);
                }
            }

            if (refund != null)
            {
                if (@event == "refund.success")
                {
                    refund.Status = "completed";
                    refund.CompletedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(refundId)) refund.ExternalRefundId = refundId;

                    db.RefundAuditLogs.Add(new RefundAuditLog
                    {
                        DepositRefundId = refund.Id,
                        Action = "completed",
                        OldStatus = "processing",
                        NewStatus = "completed",
                        PerformedByUserId = null,
                        Notes = "Refund completed via provider webhook"
                    });

                    // Notify renter
                    if (refund.Booking?.Renter != null)
                    {
                        var notifySvc = context.RequestServices.GetRequiredService<INotificationService>();
                        var job = new Models.NotificationJob
                        {
                            BookingId = refund.BookingId,
                            TargetUserId = refund.Booking.Renter.Id,
                            ChannelsJson = JsonSerializer.Serialize(new[]{"inapp","email","whatsapp"}),
                            TemplateName = "deposit_refund_processed",
                            Subject = "Deposit Refund Processed",
                            Message = $"Your refund for booking {refund.Booking.BookingReference} has been completed. Amount: {refund.Currency} {refund.Amount:F2}",
                            MetadataJson = JsonSerializer.Serialize(new { amount = refund.Amount, currency = refund.Currency, bookingReference = refund.Booking.BookingReference }),
                            SendImmediately = true
                        };
                        await notifySvc.CreateNotificationJobAsync(job);
                    }
                }
                else
                {
                    refund.Status = "failed";
                    refund.ErrorMessage = data.ToString();

                    db.RefundAuditLogs.Add(new RefundAuditLog
                    {
                        DepositRefundId = refund.Id,
                        Action = "failed",
                        OldStatus = "processing",
                        NewStatus = "failed",
                        PerformedByUserId = null,
                        Notes = "Refund failed via provider webhook"
                    });

                    if (refund.Booking?.Renter != null)
                    {
                        var notifySvc = context.RequestServices.GetRequiredService<INotificationService>();
                        var job = new Models.NotificationJob
                        {
                            BookingId = refund.BookingId,
                            TargetUserId = refund.Booking.Renter.Id,
                            ChannelsJson = JsonSerializer.Serialize(new[]{"inapp","email","whatsapp"}),
                            Subject = "Refund failed",
                            Message = $"Automatic refund for booking {refund.Booking.BookingReference} failed. Please contact support.",
                            SendImmediately = true
                        };
                        await notifySvc.CreateNotificationJobAsync(job);
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        // Handle transfer (payout) events
        if ((@event == "transfer.success" || @event == "transfer.failed") )
        {
            string? transferRef = null;
            string? transferCode = null;
            try
            {
                transferRef = data.GetProperty("reference").GetString();
                transferCode = data.TryGetProperty("transfer_code", out var tc) ? tc.GetString() : null;
            }
            catch { }

            GhanaHybridRentalApi.Models.Payout? payout = null;
            if (!string.IsNullOrWhiteSpace(transferRef))
                payout = await db.Payouts.FirstOrDefaultAsync(p => p.Reference == transferRef || p.ExternalPayoutId == transferRef);

            if (payout == null && !string.IsNullOrWhiteSpace(transferCode))
                payout = await db.Payouts.FirstOrDefaultAsync(p => p.ExternalPayoutId == transferCode);

            if (payout != null)
            {
                if (@event == "transfer.success")
                {
                    payout.Status = "completed";
                    payout.CompletedAt = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(transferCode)) payout.ExternalPayoutId = transferCode;

                    db.PayoutAuditLogs.Add(new PayoutAuditLog
                    {
                        PayoutId = payout.Id,
                        Action = "completed",
                        OldStatus = "processing",
                        NewStatus = "completed",
                        PerformedByUserId = null,
                        Notes = "Payout completed via provider webhook"
                    });

                    if (payout.Owner != null)
                    {
                        var notifySvc = context.RequestServices.GetRequiredService<INotificationService>();
                        await notifySvc.SendOwnerNotificationAsync(payout.Owner, "Payout completed", $"Your payout {payout.Reference} of {payout.Currency} {payout.Amount:F2} has been completed.");
                    }
                }
                else
                {
                    payout.Status = "failed";
                    payout.ErrorMessage = data.ToString();

                    db.PayoutAuditLogs.Add(new PayoutAuditLog
                    {
                        PayoutId = payout.Id,
                        Action = "failed",
                        OldStatus = "processing",
                        NewStatus = "failed",
                        PerformedByUserId = null,
                        Notes = "Payout failed via provider webhook"
                    });

                    if (payout.Owner != null)
                    {
                        var notifySvc = context.RequestServices.GetRequiredService<INotificationService>();
                        await notifySvc.SendOwnerNotificationAsync(payout.Owner, "Payout failed", $"Your payout {payout.Reference} of {payout.Currency} {payout.Amount:F2} failed. Please contact support.");
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        return Results.Ok();
    }

    private static Task<IResult> PaystackWebhookVerificationAsync(HttpContext context)
    {
        // Paystack sends GET request to verify webhook URL during setup
        // Just return 200 OK to confirm the endpoint is accessible
        return Task.FromResult(Results.Ok(new { message = "Paystack webhook endpoint verified" }));
    }
}

public record WebhookNotification(
    string Event,
    Guid? BookingId,
    Dictionary<string, object>? Data
);

public record PartnerBookingRequest(
    Guid VehicleId,
    DateTime PickupDateTime,
    DateTime ReturnDateTime,
    bool WithDriver,
    string RenterEmail,
    string? RenterPhone,
    string RenterName,
    string PaymentMethod,
    Guid? ProtectionPlanId,  // Optional - will use default if not provided
    string? PromoCode        // Optional - promo code for discount
);

public record PartnerPromoValidationRequest(
    string PromoCode,
    Guid VehicleId,
    decimal BookingAmount,
    int RentalDays
);

// Helper method to validate partner API key with expiry checking
static class PartnerAuthHelper
{
    public static async Task<(bool isValid, IntegrationPartner? partner, string? errorMessage)> ValidatePartnerApiKeyAsync(
        string? apiKey, 
        AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, null, "API key is required");

        var partner = await db.IntegrationPartners
            .FirstOrDefaultAsync(p => p.ApiKey == apiKey);

        if (partner is null)
            return (false, null, "Invalid API key");

        if (!partner.Active)
            return (false, null, "Partner account is not active");

        // Check API key expiry
        if (partner.ApiKeyExpiresAt.HasValue && partner.ApiKeyExpiresAt.Value < DateTime.UtcNow)
            return (false, null, $"API key expired on {partner.ApiKeyExpiresAt.Value:yyyy-MM-dd}. Please contact support to renew.");

        // Update last used timestamp
        partner.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return (true, partner, null);
    }
}

