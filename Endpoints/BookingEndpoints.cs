using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Extensions; // Absolutize URL helper

namespace GhanaHybridRentalApi.Endpoints;

public static class BookingEndpoints
{
    private static ILogger<object>? _logger;
    
    public static void MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        _logger = app.ServiceProvider.GetRequiredService<ILogger<object>>();
        // Booking creation and listing
        app.MapGet("/api/v1/bookings", GetBookingsAsync)
            .RequireAuthorization();

        app.MapGet("/api/v1/bookings/{bookingId:guid}", GetBookingByIdAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/bookings", CreateBookingAsync);
        app.MapGet("/api/v1/bookings/calculate-total", CalculateBookingTotalAsync);

        // Search bookings (owner/admin) - by phone, name, QR token or booking reference
        app.MapGet("/api/v1/bookings/lookup", SearchBookingsAsync)
            .RequireAuthorization();

        app.MapPut("/api/v1/bookings/{bookingId:guid}/status", UpdateBookingStatusAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/bookings/{bookingId:guid}/cancel", CancelBookingAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/bookings/{bookingId:guid}/refund-deposit", RefundDepositAsync)
            .RequireAuthorization();

        // Search bookings by phone/first/last name/QR token - accessible to owner/admin
        app.MapGet("/api/v1/bookings/find", SearchBookingsAsync)
            .RequireAuthorization();

        // Inspection links
        app.MapPost("/api/v1/bookings/{bookingId:guid}/inspection-links", GenerateInspectionLinksAsync)
            .RequireAuthorization();
        
        // Extend booking (renter-initiated)
        app.MapPost("/api/v1/bookings/{bookingId:guid}/extend", ExtendBookingAsync)
            .RequireAuthorization();

        // QR payload endpoint for frontend QR generation
        app.MapGet("/api/v1/bookings/{bookingId:guid}/qr", GetBookingQrPayloadAsync)
            .RequireAuthorization();

        // Trip start/complete endpoints (Owner quick check-in/out)
        app.MapPost("/api/v1/bookings/{bookingId:guid}/start-trip", StartTripAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/bookings/{bookingId:guid}/complete-trip", CompleteTripAsync)
            .RequireAuthorization();

        // Resend booking confirmation email
        app.MapPost("/api/v1/bookings/{bookingId:guid}/email/confirmation", ResendConfirmationEmailAsync)
            .RequireAuthorization();

        // Guest booking management endpoints
        app.MapPut("/api/v1/bookings/guest/{bookingReference}/contact", UpdateGuestContactAsync)
            .AllowAnonymous();

        app.MapPut("/api/v1/bookings/{bookingId:guid}/guest-contact", UpdateGuestContactByStaffAsync)
            .RequireAuthorization();

        // Retrieve booking by reference (for unpaid bookings)
        app.MapGet("/api/v1/bookings/reference/{bookingReference}", GetBookingByReferenceAsync)
            .AllowAnonymous();

        // Sign rental agreement
        app.MapPost("/api/v1/bookings/{bookingId:guid}/sign-agreement", SignAgreementAsync)
            .AllowAnonymous();

        // Initiate payment for existing booking
        app.MapPost("/api/v1/bookings/{bookingId:guid}/initiate-payment", InitiatePaymentAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetBookingsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? vehicleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var userRole = user.Role ?? string.Empty;

        var query = db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Owner)
                    .ThenInclude(o => o!.OwnerProfile)
            .Include(b => b.ProtectionPlan)
            .Include(b => b.PaymentTransaction)
            .AsQueryable();

        // Filter based on role
        if (userRole == "renter")
            query = query.Where(b => b.RenterId == userId);
        else if (userRole == "owner")
            query = query.Where(b => b.OwnerId == userId);
        else if (userRole == "driver")
            query = query.Where(b => b.DriverId == userId);
        // Admin sees all

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status.ToLowerInvariant());

        if (vehicleId.HasValue)
            query = query.Where(b => b.VehicleId == vehicleId.Value);

        var total = await query.CountAsync();
        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = bookings.Select(b => new BookingResponse(b))
        });
    }

    private static async Task<IResult> GetBookingByIdAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Owner)
                    .ThenInclude(o => o!.OwnerProfile)
            .Include(b => b.ProtectionPlan)
            .Include(b => b.PaymentTransaction)
            .Include(b => b.Renter).ThenInclude(u => u!.RenterProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check access rights
        var role = user.Role ?? string.Empty;
        var hasAccess = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
                       booking.RenterId == userId ||
                       booking.OwnerId == userId ||
                       booking.DriverId == userId;

        if (!hasAccess)
            return Results.Forbid();

        return Results.Ok(new BookingResponse(booking));
    }

    private static async Task<IResult> UpdateBookingStatusAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] UpdateBookingStatusRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var userRole = user.Role ?? string.Empty;
        // Only admin or owner can update booking status
        if (userRole != "admin" && booking.OwnerId != userId)
            return Results.Forbid();

        var validStatuses = new[] { "pending_payment", "confirmed", "ongoing", "completed", "cancelled", "no_show" };
        if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid status" });

        var oldStatus = booking.Status;
        booking.Status = request.Status.ToLowerInvariant();

        // Auto-create deposit refund when booking is marked as completed
        if (booking.Status == "completed" && oldStatus != "completed" && booking.DepositAmount > 0)
        {
            // Check if refund already exists
            var existingRefund = await db.DepositRefunds.FirstOrDefaultAsync(r => r.BookingId == bookingId);
            
            if (existingRefund == null)
            {
                var refund = new DepositRefund
                {
                    BookingId = bookingId,
                    Amount = booking.DepositAmount,
                    Currency = booking.Currency,
                    PaymentMethod = booking.PaymentMethod,
                    Status = "pending",
                    DueDate = DateTime.UtcNow.AddDays(2), // Due in 2 days
                    Reference = $"REF-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Notes = "Auto-created deposit refund after booking completion"
                };

                db.DepositRefunds.Add(refund);

                // Create audit log
                var auditLog = new RefundAuditLog
                {
                    DepositRefundId = refund.Id,
                    Action = "created",
                    OldStatus = "",
                    NewStatus = "pending",
                    PerformedByUserId = userId,
                    Notes = "Auto-created when booking status changed to completed"
                };

                db.RefundAuditLogs.Add(auditLog);
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new { booking.Id, booking.Status });
    }

    private static async Task<IResult> CancelBookingAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .ThenInclude(v => v!.Category)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only renter or admin can cancel
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Role != "admin" && booking.RenterId != userId)
            return Results.Forbid();

        // Can only cancel if not completed or already cancelled
        if (booking.Status == "completed" || booking.Status == "cancelled")
            return Results.BadRequest(new { error = "Cannot cancel this booking" });

        booking.Status = "cancelled";

        // Calculate refund based on refund policies
        decimal refundAmount = 0m;
        decimal depositRefund = 0m;
        string? refundPolicyName = null;

        if (booking.PaymentStatus == "paid")
        {
            var hoursUntilPickup = (booking.PickupDateTime - DateTime.UtcNow).TotalHours;
            var categoryId = booking.Vehicle?.CategoryId;

            // Find applicable refund policy
            var query = db.RefundPolicies
                .Where(p => p.IsActive && p.HoursBeforePickup <= hoursUntilPickup);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value || p.CategoryId == null);
            }
            else
            {
                query = query.Where(p => p.CategoryId == null);
            }

            var policy = await query
                .OrderBy(p => p.Priority)
                .ThenByDescending(p => p.HoursBeforePickup)
                .FirstOrDefaultAsync();

            if (policy != null)
            {
                refundAmount = booking.TotalAmount * (policy.RefundPercentage / 100m);
                depositRefund = policy.RefundDeposit ? booking.DepositAmount : 0m;
                refundPolicyName = policy.PolicyName;
            }

            var totalRefund = refundAmount + depositRefund;

            if (totalRefund > 0)
            {
                var refundTransaction = new PaymentTransaction
                {
                    UserId = booking.RenterId,
                    BookingId = booking.Id,
                    Type = "refund",
                    Status = "pending",
                    Amount = totalRefund,
                    Currency = booking.Currency,
                    Method = booking.PaymentMethod,
                    Reference = $"REFUND-{Guid.NewGuid().ToString("N")[..12].ToUpper()}"
                };

                db.PaymentTransactions.Add(refundTransaction);
                booking.PaymentStatus = totalRefund >= (booking.TotalAmount + booking.DepositAmount) ? "refunded" : "partial_refund";
            }
            else
            {
                booking.PaymentStatus = "non_refundable";
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            booking.Id,
            booking.Status,
            booking.PaymentStatus,
            refundAmount,
            depositRefund,
            totalRefund = refundAmount + depositRefund,
            refundPolicyApplied = refundPolicyName,
            message = "Booking cancelled successfully"
        });
    }

    private static async Task<IResult> RefundDepositAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        Services.IRefundService refundService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var userRole = user.Role ?? string.Empty;

        var booking = await db.Bookings
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
        
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only owner or admin can process deposit refund
        if (userRole != "admin" && booking.OwnerId != userId)
            return Results.Forbid();

        if (booking.Status != "completed")
            return Results.BadRequest(new { error = "Can only refund deposit for completed bookings" });

        // Check if deposit already refunded
        var existingRefund = await db.PaymentTransactions
            .AnyAsync(t => t.BookingId == bookingId && t.Type == "refund" && t.Status == "completed");

        if (existingRefund)
            return Results.BadRequest(new { error = "Deposit already refunded" });

        // Get renter contact info
        var renterPhone = booking.Renter?.Phone ?? booking.GuestPhone;
        var renterEmail = booking.Renter?.Email ?? booking.GuestEmail;

        if (string.IsNullOrWhiteSpace(renterPhone))
            return Results.BadRequest(new { error = "Renter phone number not found. Cannot process refund." });

        // Create pending refund transaction
        var refundTransaction = new PaymentTransaction
        {
            UserId = booking.RenterId,
            BookingId = booking.Id,
            Type = "refund",
            Status = "pending",
            Amount = booking.DepositAmount,
            Currency = booking.Currency,
            Method = "momo",
            Reference = $"REFUND-{bookingId.ToString("N")[..12].ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        db.PaymentTransactions.Add(refundTransaction);
        await db.SaveChangesAsync();

        // Process actual refund via Paystack
        var (success, transferCode, errorMessage) = await refundService.ProcessDepositRefundAsync(
            booking.Id,
            booking.RenterId,
            booking.DepositAmount,
            booking.Currency,
            renterPhone,
            renterEmail);

        if (success)
        {
            // Update transaction to completed
            refundTransaction.Status = "completed";
            refundTransaction.CompletedAt = DateTime.UtcNow;
            refundTransaction.ExternalTransactionId = transferCode;
            
            // Update payment status
            booking.PaymentStatus = booking.PaymentStatus == "paid" ? "partial_refund" : "refunded";

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                bookingId = booking.Id,
                refundAmount = booking.DepositAmount,
                transactionId = refundTransaction.Id,
                transferCode = transferCode,
                message = "Deposit refund processed successfully via mobile money"
            });
        }
        else
        {
            // Mark transaction as failed
            refundTransaction.Status = "failed";
            refundTransaction.ErrorMessage = errorMessage;
            await db.SaveChangesAsync();

            return Results.BadRequest(new
            {
                error = "Refund failed",
                details = errorMessage,
                transactionId = refundTransaction.Id,
                message = "Failed to process refund. Transaction has been logged."
            });
        }
    }

    private static async Task<IResult> CreateBookingAsync(
        ClaimsPrincipal principal,
        [FromBody] JsonElement body,
        AppDbContext db,
        IAppConfigService configService,
        IPromoCodeService promoCodeService,
        HttpContext context)
    {
        // Allow guest booking flow. If request contains "guestPhone" use guest flow, otherwise expect authenticated renter.
        CreateBookingRequest? authRequest = null;
        CreateBookingRequestGuest? guestRequest = null;

        try
        {
            // Support guest booking detection by presence of either guestPhone OR guestEmail in the payload.
            // Previously only guestPhone was checked which caused requests with only guestEmail to be
            // treated as authenticated requests and return 401 when no token was present.
            if (body.ValueKind == JsonValueKind.Object && (body.TryGetProperty("guestPhone", out _) || body.TryGetProperty("guestEmail", out _)))
            {
                guestRequest = JsonSerializer.Deserialize<CreateBookingRequestGuest>(body.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                authRequest = JsonSerializer.Deserialize<CreateBookingRequest>(body.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch
        {
            return Results.BadRequest(new { error = "Invalid request payload" });
        }

        Guid renterId;
        User? renterUser = null;

        if (authRequest != null)
        {
            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out renterId))
                return Results.Unauthorized();

            renterUser = await db.Users.FirstOrDefaultAsync(u => u.Id == renterId);
            if (renterUser is null)
                return Results.Unauthorized();
        }
        else if (guestRequest != null)
        {
            // Require at least phone or email
            if (string.IsNullOrWhiteSpace(guestRequest.GuestPhone) && string.IsNullOrWhiteSpace(guestRequest.GuestEmail))
                return Results.BadRequest(new { error = "Guest phone or email is required" });

            // Normalize phone if present
            string? normalizedPhone = null;
            if (!string.IsNullOrWhiteSpace(guestRequest.GuestPhone))
            {
                normalizedPhone = NormalizePhoneNumber(guestRequest.GuestPhone);
            }

            // Try to find existing user by phone or email
            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                renterUser = await db.Users.FirstOrDefaultAsync(u => u.Phone == normalizedPhone);
            }

            if (renterUser == null && !string.IsNullOrWhiteSpace(guestRequest.GuestEmail))
            {
                renterUser = await db.Users.FirstOrDefaultAsync(u => u.Email == guestRequest.GuestEmail);
            }

            if (renterUser == null)
            {
                // Create provisional renter user
                renterUser = new User
                {
                    Phone = normalizedPhone,
                    Email = guestRequest.GuestEmail,
                    FirstName = guestRequest.GuestFirstName,
                    LastName = guestRequest.GuestLastName,
                    Role = "renter",
                    Status = "pending",
                    PhoneVerified = false,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(renterUser);
                db.RenterProfiles.Add(new RenterProfile
                {
                    UserId = renterUser.Id,
                    FullName = $"{guestRequest.GuestFirstName} {guestRequest.GuestLastName}".Trim(),
                    DriverLicenseNumber = guestRequest.GuestDriverLicenseNumber,
                    DriverLicenseExpiryDate = guestRequest.GuestDriverLicenseExpiryDate,
                    DriverLicensePhotoUrl = guestRequest.GuestDriverLicensePhotoUrl,
                    VerificationStatus = string.IsNullOrWhiteSpace(guestRequest.GuestDriverLicenseNumber) ? "unverified" : "basic_verified"
                });

                await db.SaveChangesAsync();
            }

            renterId = renterUser.Id;
        }
        else
        {
            return Results.BadRequest(new { error = "Missing booking data" });
        }

        // Ensure renter profile exists
        var renterProfile = await db.RenterProfiles.FirstOrDefaultAsync(r => r.UserId == renterId);
        if (renterProfile == null)
        {
            // Create RenterProfile with user's name information
            var fullName = renterUser != null 
                ? $"{renterUser.FirstName} {renterUser.LastName}".Trim() 
                : "Guest User";
            
            renterProfile = new RenterProfile 
            { 
                UserId = renterId, 
                FullName = string.IsNullOrWhiteSpace(fullName) ? "Guest User" : fullName,
                VerificationStatus = "unverified" 
            };
            db.RenterProfiles.Add(renterProfile);
            await db.SaveChangesAsync();
        }

        // For guest flow, allow driver's license details in the request to satisfy self-drive requirement
        // Ensure one of the request variants is present (auth or guest)
        if (authRequest == null && guestRequest == null)
            return Results.BadRequest(new { error = "Missing booking data" });

        // Resolve request values into local variables for unified processing
        Guid rVehicleId;
        DateTime rPickup, rReturn;
        bool rWithDriver;
        Guid? rDriverId;
        Guid? rInsurancePlanId;
        Guid? rProtectionPlanId;
        object? rPickupLocation;
        object? rReturnLocation;
        string? rPaymentMethod;
        string? rPromoCode;

        if (authRequest != null)
        {
            rVehicleId = authRequest.VehicleId;
            rPickup = authRequest.PickupDateTime;
            rReturn = authRequest.ReturnDateTime;
            rWithDriver = authRequest.WithDriver;
            rDriverId = authRequest.DriverId;
            rInsurancePlanId = authRequest.InsurancePlanId;
            rProtectionPlanId = authRequest.ProtectionPlanId;
            rPickupLocation = authRequest.PickupLocation;
            rReturnLocation = authRequest.ReturnLocation;
            rPaymentMethod = authRequest.PaymentMethod;
            rPromoCode = authRequest.PromoCode;
        }
        else
        {
            // guestRequest guaranteed non-null due to earlier check
            rVehicleId = guestRequest!.VehicleId;
            rPickup = guestRequest.PickupDateTime;
            rReturn = guestRequest.ReturnDateTime;
            rWithDriver = guestRequest.WithDriver;
            rDriverId = guestRequest.DriverId;
            rInsurancePlanId = guestRequest.InsurancePlanId;
            rProtectionPlanId = guestRequest.ProtectionPlanId;
            rPickupLocation = guestRequest.PickupLocation;
            rReturnLocation = guestRequest.ReturnLocation;
            rPaymentMethod = guestRequest.PaymentMethod;
            rPromoCode = guestRequest.PromoCode;
        }

        if (rPickup >= rReturn)
            return Results.BadRequest(new { error = "ReturnDateTime must be after PickupDateTime" });

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == rVehicleId && v.DeletedAt == null);
        if (vehicle is null || vehicle.Status != "active")
            return Results.BadRequest(new { error = "Vehicle not available" });

        // CRITICAL: Check vehicle availability to prevent double booking
        // Add 4-hour buffer after each booking for cleaning/inspection/turnaround
        var pickupWithBuffer = rPickup.AddHours(-4);
        var returnWithBuffer = rReturn.AddHours(4);
        
        var hasConflictingBooking = await db.Bookings
            .Where(b => b.VehicleId == rVehicleId &&
                       b.Status != "cancelled" &&
                       b.Status != "completed" &&
                       b.PickupDateTime < returnWithBuffer &&
                       b.ReturnDateTime > pickupWithBuffer)
            .AnyAsync();

        if (hasConflictingBooking)
        {
            return Results.BadRequest(new 
            { 
                error = "Vehicle not available for selected dates",
                message = "This vehicle is already booked for the dates you selected or needs a 4-hour buffer for cleaning and inspection. Please choose different dates or another vehicle.",
                code = "VEHICLE_ALREADY_BOOKED"
            });
        }

        var category = vehicle.CategoryId.HasValue
            ? await db.CarCategories.FirstOrDefaultAsync(c => c.Id == vehicle.CategoryId.Value)
            : null;

        if (category is null)
            return Results.BadRequest(new { error = "Vehicle category not configured" });

        // Calculate rental days: exact time difference, rounded up (matches frontend Math.ceil)
        var totalDays = (int)Math.Ceiling((rReturn - rPickup).TotalDays);
        if (totalDays < 1) totalDays = 1; // Safeguard: minimum 1 day

        var dailyRate = vehicle.DailyRate ?? category.DefaultDailyRate;
        var rentalAmount = dailyRate * (decimal)totalDays;
        var depositAmount = category.DefaultDepositAmount;

        // Handle driver pricing if WithDriver is true
        decimal? driverAmount = null;
        Guid? assignedDriverId = null;

        if (rWithDriver)
        {
            DriverProfile? driverProfile = null;

            if (rDriverId.HasValue)
            {
                // Specific driver requested
                driverProfile = await db.DriverProfiles
                    .FirstOrDefaultAsync(d => d.UserId == rDriverId.Value && d.Available && d.VerificationStatus == "verified");
            }
            else
            {
                // Find any available driver
                driverProfile = await db.DriverProfiles
                    .Where(d => d.Available && d.VerificationStatus == "verified")
                    .OrderByDescending(d => d.AverageRating)
                    .FirstOrDefaultAsync();
            }

            if (driverProfile == null)
                return Results.BadRequest(new { error = "No available drivers found" });

            var driverDailyRate = driverProfile.DailyRate ?? 45.0m; // Default rate if not set
            driverAmount = driverDailyRate * (decimal)totalDays;
            assignedDriverId = driverProfile.UserId;
        }

        InsurancePlan? insurancePlan = null;
        decimal? insuranceAmount = null;
        bool insuranceAccepted = false;

        if (rInsurancePlanId.HasValue)
        {
            insurancePlan = await db.InsurancePlans.FirstOrDefaultAsync(p => p.Id == rInsurancePlanId.Value && p.Active);
        }
        else
        {
            insurancePlan = await db.InsurancePlans.FirstOrDefaultAsync(p => p.IsDefault && p.Active);
        }

        if (insurancePlan is not null)
        {
            insuranceAmount = insurancePlan.DailyPrice * (decimal)totalDays;
            insuranceAccepted = true;
        }

        // Protection plan handling (Option B)
        ProtectionPlan? protectionPlan = null;
        decimal? protectionAmount = null;

        if (rProtectionPlanId.HasValue)
        {
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.Id == rProtectionPlanId.Value && p.IsActive);
        }
        else
        {
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.IsActive && p.IsDefault);
        }

        if (protectionPlan is not null)
        {
            if (protectionPlan.PricingMode == "per_day")
            {
                protectionAmount = protectionPlan.DailyPrice * (decimal)totalDays;
            }
            else
            {
                protectionAmount = protectionPlan.FixedPrice;
            }

            protectionAmount = Math.Max(protectionPlan.MinFee, Math.Min(protectionAmount.Value, protectionPlan.MaxFee));
        }

        // Get platform fee percentage from GlobalSettings (default 15%)
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }
        
        // Calculate platform fee (only on rental amount + driver amount, not protection/insurance)
        var subtotal = rentalAmount + (driverAmount ?? 0m);
        var platformFee = subtotal * (platformFeePercentage / 100m);

        var totalAmount = rentalAmount + depositAmount + (driverAmount ?? 0m) + (insuranceAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;

        // Apply promo code if provided
        Guid? promoCodeId = null;
        decimal promoDiscountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(rPromoCode))
        {
            try
            {
                var categoryId = vehicle.CategoryId;
                var cityId = vehicle.CityId;
                
                var validationRequest = new ValidatePromoCodeDto(
                    rPromoCode,
                    totalAmount,
                    vehicle.Id,
                    categoryId,
                    cityId,
                    (int)totalDays
                );

                var validation = await promoCodeService.ValidatePromoCodeAsync(rPromoCode, renterId, validationRequest);

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
                        subtotal = rentalAmount + (driverAmount ?? 0m) + (insuranceAmount ?? 0m) + (protectionAmount ?? 0m);
                        platformFee = subtotal * (platformFeePercentage / 100m);
                        totalAmount = rentalAmount + depositAmount + (driverAmount ?? 0m) + (insuranceAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;
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

        // Generate booking reference
        var bookingReference = $"RV-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var booking = new Booking
        {
            RenterId = renterId,
            VehicleId = vehicle.Id,
            OwnerId = vehicle.OwnerId,
            PickupDateTime = rPickup,
            ReturnDateTime = rReturn,
            PickupLocationJson = rPickupLocation is null ? null : JsonSerializer.Serialize(rPickupLocation),
            ReturnLocationJson = rReturnLocation is null ? null : JsonSerializer.Serialize(rReturnLocation),
            WithDriver = rWithDriver,
            DriverId = assignedDriverId,
            DriverAmount = driverAmount,
            BookingReference = bookingReference,
            Currency = "GHS",
            RentalAmount = rentalAmount,
            DepositAmount = depositAmount,
            InsurancePlanId = insurancePlan?.Id,
            InsuranceAmount = insuranceAmount,
            ProtectionPlanId = protectionPlan?.Id,
            ProtectionAmount = protectionAmount,
            ProtectionSnapshotJson = protectionPlan is not null ? JsonSerializer.Serialize(new { protectionPlan.Code, protectionPlan.Name, protectionPlan.DailyPrice, protectionPlan.FixedPrice, protectionPlan.MinFee, protectionPlan.MaxFee, protectionPlan.IncludesMinorDamageWaiver, protectionPlan.MinorWaiverCap, protectionPlan.Deductible, protectionPlan.ExcludesJson }) : null,
            InsuranceAccepted = insuranceAccepted,
            PlatformFee = platformFee,
            TotalAmount = totalAmount,
            PromoCodeId = promoCodeId,
            PromoDiscountAmount = promoDiscountAmount,
            PaymentMethod = rPaymentMethod.ToLowerInvariant(),
            Status = "pending_payment",
            PaymentStatus = "unpaid",
            CreatedAt = DateTime.UtcNow,
            // Populate guest fields for guest bookings
            GuestEmail = guestRequest?.GuestEmail,
            GuestPhone = guestRequest?.GuestPhone,
            GuestFirstName = guestRequest?.GuestFirstName,
            GuestLastName = guestRequest?.GuestLastName
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // Record promo code usage if applied
        if (promoCodeId.HasValue && !string.IsNullOrWhiteSpace(rPromoCode))
        {
            try
            {
                await promoCodeService.ApplyPromoCodeAsync(
                    rPromoCode,
                    renterId,
                    booking.Id,
                    totalAmount + promoDiscountAmount, // Original amount before discount
                    "renter"
                );
            }
            catch (Exception ex)
            {
                // Log error but don't fail the booking
                Console.WriteLine($"Failed to record promo code usage: {ex.Message}");
            }
        }

        // Send "booking reserved" notifications to customer and owner (pending payment)
        try
        {
            var notificationService = context.RequestServices.GetService<INotificationService>();
            
            if (notificationService != null)
            {
                // Load related entities for notifications
                await db.Entry(booking).Reference(b => b.Renter).LoadAsync();
                await db.Entry(booking).Reference(b => b.Vehicle).LoadAsync();
                
                // Send "booking reserved" email to customer (not yet paid)
                await notificationService.SendBookingConfirmationToCustomerAsync(booking);
                
                // Send "booking reserved" email to owner (pending payment)
                await notificationService.SendBookingConfirmationToOwnerAsync(booking);
                
                // Schedule pickup and return reminders
                var pickupReminderTime = booking.PickupDateTime.AddDays(-1); // 24 hours before pickup
                var returnReminderTime = booking.ReturnDateTime.AddDays(-1); // 24 hours before return
                
                // Create pickup reminder job
                if (pickupReminderTime > DateTime.UtcNow)
                {
                    var pickupReminderJob = new Models.NotificationJob
                    {
                        TargetUserId = booking.RenterId,
                        TemplateName = "pickup_reminder",
                        ChannelsJson = JsonSerializer.Serialize(new[] { "inapp", "email", "whatsapp" }),
                        MetadataJson = JsonSerializer.Serialize(new { bookingId = booking.Id }),
                        ScheduledAt = pickupReminderTime,
                        SendImmediately = false,
                        Status = "pending",
                        Subject = "Pickup Reminder",
                        Message = $"Reminder: Your booking {booking.BookingReference} pickup is tomorrow"
                    };
                    await notificationService.CreateNotificationJobAsync(pickupReminderJob);
                }
                
                // Create return reminder job
                if (returnReminderTime > DateTime.UtcNow)
                {
                    var returnReminderJob = new Models.NotificationJob
                    {
                        TargetUserId = booking.RenterId,
                        TemplateName = "return_reminder",
                        ChannelsJson = JsonSerializer.Serialize(new[] { "inapp", "email", "whatsapp" }),
                        MetadataJson = JsonSerializer.Serialize(new { bookingId = booking.Id }),
                        ScheduledAt = returnReminderTime,
                        SendImmediately = false,
                        Status = "pending",
                        Subject = "Return Reminder",
                        Message = $"Reminder: Your booking {booking.BookingReference} return is tomorrow"
                    };
                    await notificationService.CreateNotificationJobAsync(returnReminderJob);
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "Error sending booking notifications for {BookingRef}", booking.BookingReference);
            }
            // Don't fail the booking creation if notifications fail
        }

        // Check if guest has previous bookings and provide gentle suggestion
        string? accountSuggestion = null;
        if (guestRequest != null)
        {
            var hasExistingBookings = false;
            
            if (!string.IsNullOrWhiteSpace(guestRequest.GuestEmail))
            {
                hasExistingBookings = await db.Bookings
                    .AnyAsync(b => b.GuestEmail == guestRequest.GuestEmail && b.Id != booking.Id);
            }
            else if (!string.IsNullOrWhiteSpace(guestRequest.GuestPhone))
            {
                var normalizedPhone = NormalizePhoneNumber(guestRequest.GuestPhone);
                hasExistingBookings = await db.Bookings
                    .AnyAsync(b => b.GuestPhone == normalizedPhone && b.Id != booking.Id);
            }
            
            if (hasExistingBookings)
            {
                accountSuggestion = "💡 We noticed you've booked with us before! Create an account to track all your bookings and enjoy faster checkout.";
            }
        }

        // If this was a guest booking and the linked user has no password, tell frontend to prompt for account claim
        if (guestRequest != null && renterUser != null && string.IsNullOrWhiteSpace(renterUser.PasswordHash))
        {
            return Results.Created($"/api/v1/bookings/{booking.Id}", new
            {
                booking = new BookingResponse(booking),
                account = new { exists = true, requirePasswordSetup = true, userId = renterUser.Id },
                accountSuggestion = accountSuggestion,
                manageBookingUrl = $"https://ryverental.info/bookings/{booking.BookingReference}"
            });
        }

        return Results.Created($"/api/v1/bookings/{booking.Id}", new
        {
            booking = new BookingResponse(booking),
            accountSuggestion = accountSuggestion,
            manageBookingUrl = $"https://ryverental.info/bookings/{booking.BookingReference}"
        });
    }

    private static async Task<IResult> CalculateBookingTotalAsync(
        AppDbContext db,
        IAppConfigService configService,
        [FromQuery] Guid vehicleId,
        [FromQuery(Name = "pickupDateTime")] DateTime? pickupDateTime,
        [FromQuery(Name = "returnDateTime")] DateTime? returnDateTime,
        [FromQuery(Name = "startDate")] DateTime? startDate,
        [FromQuery(Name = "endDate")] DateTime? endDate,
        [FromQuery] bool withDriver = false,
        [FromQuery] Guid? driverId = null,
        [FromQuery] Guid? insurancePlanId = null,
        [FromQuery] Guid? protectionPlanId = null)
    {
        // Support both `pickupDateTime`/`returnDateTime` and `startDate`/`endDate` query param names
        var pickup = pickupDateTime ?? startDate;
        var ret = returnDateTime ?? endDate;

        if (!pickup.HasValue || !ret.HasValue)
            return Results.BadRequest(new { error = "Missing pickup or return dates. Provide pickupDateTime/returnDateTime or startDate/endDate." });

        var pickupDt = pickup.Value;
        var returnDt = ret.Value;
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.DeletedAt == null);
        if (vehicle is null || vehicle.Status != "active")
            return Results.BadRequest(new { error = "Vehicle not available" });

        var category = vehicle.CategoryId.HasValue
            ? await db.CarCategories.FirstOrDefaultAsync(c => c.Id == vehicle.CategoryId.Value)
            : null;

        if (category is null)
            return Results.BadRequest(new { error = "Vehicle category not configured" });

        // Calculate rental days: exact time difference, rounded up (matches frontend Math.ceil)
        // Example: Jan 7 5am to Jan 8 5am = 1.0 days → 1 day
        // Example: Jan 7 5am to Jan 8 1pm = 1.33 days → 2 days
        var totalDays = (int)Math.Ceiling((returnDt - pickupDt).TotalDays);
        if (totalDays < 1) totalDays = 1; // Safeguard: minimum 1 day

        var dailyRate = vehicle.DailyRate ?? category.DefaultDailyRate;
        var rentalAmount = dailyRate * (decimal)totalDays;
        var depositAmount = category.DefaultDepositAmount;

        decimal? driverAmount = null;
        if (withDriver)
        {
            DriverProfile? driverProfile = null;
            if (driverId.HasValue)
            {
                driverProfile = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == driverId.Value && d.Available && d.VerificationStatus == "verified");
            }
            else
            {
                driverProfile = await db.DriverProfiles.Where(d => d.Available && d.VerificationStatus == "verified").OrderByDescending(d => d.AverageRating).FirstOrDefaultAsync();
            }
            if (driverProfile != null)
            {
                var driverDailyRate = driverProfile.DailyRate ?? 45.0m;
                driverAmount = driverDailyRate * (decimal)totalDays;
            }
        }

        InsurancePlan? insurancePlan = null;
        decimal? insuranceAmount = null;
        if (insurancePlanId.HasValue)
        {
            insurancePlan = await db.InsurancePlans.FirstOrDefaultAsync(p => p.Id == insurancePlanId.Value && p.Active);
        }
        else
        {
            insurancePlan = await db.InsurancePlans.FirstOrDefaultAsync(p => p.IsDefault && p.Active);
        }
        if (insurancePlan is not null)
            insuranceAmount = insurancePlan.DailyPrice * (decimal)totalDays;

        ProtectionPlan? protectionPlan = null;
        decimal? protectionAmount = null;
        if (protectionPlanId.HasValue)
        {
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.Id == protectionPlanId.Value && p.IsActive);
        }
        else
        {
            protectionPlan = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.IsActive && p.IsDefault);
        }
        if (protectionPlan is not null)
        {
            if (protectionPlan.PricingMode == "per_day")
            {
                protectionAmount = protectionPlan.DailyPrice * (decimal)totalDays;
            }
            else
            {
                protectionAmount = protectionPlan.FixedPrice;
            }
            protectionAmount = Math.Max(protectionPlan.MinFee, Math.Min(protectionAmount.Value, protectionPlan.MaxFee));
        }

        // Get platform fee percentage from GlobalSettings (default 15%)
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }
        
        // Calculate platform fee (only on rental amount + driver amount, not protection/insurance)
        var subtotal = rentalAmount + (driverAmount ?? 0m);
        var platformFee = subtotal * (platformFeePercentage / 100m);
        var total = rentalAmount + depositAmount + (driverAmount ?? 0m) + (insuranceAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;

        return Results.Ok(new
        {
            vehicleId,
            totalDays,
            rentalAmount,
            depositAmount,
            driverAmount,
            insuranceAmount,
            protectionAmount,
            platformFee,
            totalAmount = total
        });
    }

    public class ExtendBookingRequest
    {
        public DateTime NewReturnDateTime { get; set; }
    }

    private static async Task<IResult> ExtendBookingAsync(
        Guid bookingId,
        [FromBody] ExtendBookingRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.Include(b => b.Vehicle).Include(b => b.Vehicle!.Category).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.RenterId != userId)
            return Results.Forbid();

        if (booking.Status == "completed" || booking.Status == "cancelled")
            return Results.BadRequest(new { error = "Cannot extend this booking" });

        var newReturn = request.NewReturnDateTime;
        if (newReturn <= booking.ReturnDateTime)
            return Results.BadRequest(new { error = "New return must be after current return" });

        // Check conflicts for the vehicle with 4-hour buffer for turnaround
        var newReturnWithBuffer = newReturn.AddHours(4);
        var conflict = await db.Bookings.AnyAsync(b => 
            b.VehicleId == booking.VehicleId && 
            b.Id != booking.Id && 
            b.Status != "cancelled" && 
            b.Status != "completed" && 
            b.PickupDateTime < newReturnWithBuffer && 
            b.ReturnDateTime > booking.PickupDateTime);
            
        if (conflict)
            return Results.BadRequest(new { error = "Vehicle not available for the requested extension period. Another booking exists or a 4-hour buffer is required for cleaning." });

        // Calculate new total using pricing rules similar to CalculateBookingTotal
        var pickupDt = booking.PickupDateTime;
        var returnDt = newReturn;
        var category = booking.Vehicle?.Category;
        if (category is null)
            return Results.BadRequest(new { error = "Vehicle category not configured" });

        // Calculate rental days: exact time difference, rounded up (matches frontend Math.ceil)
        var totalDays = (int)Math.Ceiling((returnDt - pickupDt).TotalDays);
        if (totalDays < 1) totalDays = 1; // Safeguard: minimum 1 day
        var dailyRate = booking.Vehicle?.DailyRate ?? category.DefaultDailyRate;
        var rentalAmount = dailyRate * (decimal)totalDays;
        var depositAmount = category.DefaultDepositAmount;

        decimal? driverAmount = null;
        if (booking.WithDriver)
        {
            // use driver daily rate if assigned, otherwise a default
            decimal driverDailyRate = 45.0m;
            if (booking.DriverId.HasValue)
            {
                var driverProfile = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == booking.DriverId.Value);
                if (driverProfile != null) driverDailyRate = driverProfile.DailyRate ?? driverDailyRate;
            }

            driverAmount = driverDailyRate * (decimal)totalDays;
        }

        decimal? insuranceAmount = null;
        if (booking.InsurancePlanId.HasValue)
        {
            var p = await db.InsurancePlans.FirstOrDefaultAsync(i => i.Id == booking.InsurancePlanId.Value && i.Active);
            if (p != null) insuranceAmount = p.DailyPrice * (decimal)totalDays;
        }

        decimal? protectionAmount = null;
        if (booking.ProtectionPlanId.HasValue)
        {
            var pp = await db.ProtectionPlans.FirstOrDefaultAsync(p => p.Id == booking.ProtectionPlanId.Value && p.IsActive);
            if (pp != null)
            {
                if (pp.PricingMode == "per_day") protectionAmount = pp.DailyPrice * (decimal)totalDays;
                else protectionAmount = pp.FixedPrice;
                protectionAmount = Math.Max(pp.MinFee, Math.Min(protectionAmount.Value, pp.MaxFee));
            }
        }

        // Get platform fee percentage from GlobalSettings (default 15%)
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }
        
        // Calculate platform fee (only on rental amount + driver amount, not protection/insurance)
        var subtotal = rentalAmount + (driverAmount ?? 0m);
        var platformFee = subtotal * (platformFeePercentage / 100m);
        var newTotal = rentalAmount + depositAmount + (driverAmount ?? 0m) + (insuranceAmount ?? 0m) + (protectionAmount ?? 0m) + platformFee;

        var delta = newTotal - booking.TotalAmount;
        if (delta < 0) delta = 0m;

        return Results.Ok(new { bookingId = booking.Id, oldTotal = booking.TotalAmount, newTotal, deltaAmount = delta });
    }

    private static async Task<IResult> SearchBookingsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? phone,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? qrToken,
        [FromQuery] string? bookingReference)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var query = db.Bookings.Include(b => b.Renter).Include(b => b.Vehicle).AsQueryable();

        if (string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase))
            query = query.Where(b => b.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(bookingReference))
            query = query.Where(b => b.BookingReference == bookingReference);

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = NormalizePhoneNumber(phone);
            query = query.Where(b => b.GuestPhone == normalized || (b.Renter != null && b.Renter.Phone == normalized));
        }

        if (!string.IsNullOrWhiteSpace(firstName))
            query = query.Where(b => (b.GuestFirstName != null && b.GuestFirstName.ToLower().Contains(firstName.ToLower())) || (b.Renter != null && b.Renter.FirstName != null && b.Renter.FirstName.ToLower().Contains(firstName.ToLower())) || (b.Renter != null && b.Renter.FirstName != null && (b.Renter.FirstName + " " + (b.Renter.LastName ?? "")).ToLower().Contains(firstName.ToLower())));

        if (!string.IsNullOrWhiteSpace(lastName))
            query = query.Where(b => (b.GuestLastName != null && b.GuestLastName.ToLower().Contains(lastName.ToLower())) || (b.Renter != null && b.Renter.LastName != null && b.Renter.LastName.ToLower().Contains(lastName.ToLower())) || (b.Renter != null && b.Renter.FirstName != null && (b.Renter.FirstName + " " + (b.Renter.LastName ?? "")).ToLower().Contains(lastName.ToLower())));

        if (!string.IsNullOrWhiteSpace(qrToken))
        {
            var inspection = await db.Inspections.FirstOrDefaultAsync(i => i.MagicLinkToken == qrToken);
            if (inspection != null)
                query = query.Where(b => b.Id == inspection.BookingId);
            else
                return Results.Ok(new { data = Array.Empty<object>() });
        }

        var results = await query.OrderByDescending(b => b.CreatedAt).Take(50).ToListAsync();

        return Results.Ok(new { total = results.Count, data = results.Select(b => new BookingResponse(b)) });
    }

    // Helper to normalize Ghana phone numbers used by auth endpoints
    private static string NormalizePhoneNumber(string phone)
    {
        var normalized = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

        if (!normalized.StartsWith("233") && normalized.StartsWith("0"))
        {
            normalized = "233" + normalized.Substring(1);
        }
        else if (!normalized.StartsWith("233"))
        {
            normalized = "233" + normalized;
        }

        return normalized;
    }

    private static async Task<IResult> GenerateInspectionLinksAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        HttpContext httpContext)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var userRole = user.Role ?? string.Empty;
        var isOwner = booking.OwnerId == userId;
        var isRenter = booking.RenterId == userId;

        // Allow admins to generate inspection links for any booking
        if (userRole != "admin" && !isOwner && !isRenter)
            return Results.Forbid();

        // Check if renter has accepted rental agreement (Enterprise/Hertz style requirement)
        var agreementAccepted = await db.RentalAgreementAcceptances
            .AnyAsync(a => a.BookingId == bookingId);

        if (!agreementAccepted)
        {
            return Results.BadRequest(new
            {
                error = "Renter must accept the rental agreement before inspection links can be generated.",
                requiredAction = "GET /api/v1/bookings/{bookingId}/rental-agreement"
            });
        }

        var pickupInspection = new Inspection
        {
            BookingId = booking.Id,
            Type = "pickup",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            MagicLinkToken = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var returnInspection = new Inspection
        {
            BookingId = booking.Id,
            Type = "return",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            MagicLinkToken = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(3)
        };

        db.Inspections.Add(pickupInspection);
        db.Inspections.Add(returnInspection);

        booking.PickupInspectionId = pickupInspection.Id;
        booking.ReturnInspectionId = returnInspection.Id;

        await db.SaveChangesAsync();

        // Use frontend URL instead of API endpoint
        var frontendUrl = "https://ryverental.com";
        var pickupLink = $"{frontendUrl}/inspection/{pickupInspection.MagicLinkToken}";
        var returnLink = $"{frontendUrl}/inspection/{returnInspection.MagicLinkToken}";

        return Results.Ok(new { pickupLink, returnLink });
    }

    public record BookingQrPayloadResponse(
        Guid BookingId,
        string BookingReference,
        string PickupInspectionToken,
        string PickupUrl,
        string? DeepLink
    );

    private static async Task<IResult> GetBookingQrPayloadAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        HttpContext httpContext)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.Renter)
            .Include(b => b.PickupInspection)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only admin, owner or renter should see QR payload
        var hasAccess = string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) ||
                        booking.OwnerId == userId ||
                        booking.RenterId == userId;

        if (!hasAccess)
            return Results.Forbid();

        // Ensure a pickup inspection exists
        var pickupInspection = booking.PickupInspection;
        if (pickupInspection is null)
        {
            pickupInspection = new Inspection
            {
                BookingId = booking.Id,
                Type = "pickup",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                MagicLinkToken = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            db.Inspections.Add(pickupInspection);
            booking.PickupInspectionId = pickupInspection.Id;
            await db.SaveChangesAsync();
        }

        var pickupUrl = httpContext.Request.AbsolutizeUrl($"/inspect/{pickupInspection.MagicLinkToken}");

        // Optional: app deep-link if you register ryverental://
        var deepLink = $"ryverental://checkin?bookingId={booking.Id}&token={pickupInspection.MagicLinkToken}";

        var response = new BookingQrPayloadResponse(
            booking.Id,
            booking.BookingReference,
            pickupInspection.MagicLinkToken!,
            pickupUrl,
            deepLink
        );

        return Results.Ok(response);
    }

    public record StartTripRequest(
        int Odometer,
        double FuelLevel,
        string? Notes,
        List<string>? PhotoUrls
    );

    public record CompleteTripRequest(
        int Odometer,
        double FuelLevel,
        string? Notes,
        List<string>? PhotoUrls
    );

    private static async Task<IResult> StartTripAsync(
        Guid bookingId,
        [FromBody] StartTripRequest request,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only owner or admin can start trip
        if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) && booking.OwnerId != userId)
            return Results.Forbid();

        // Validate booking status - must be confirmed
        if (booking.Status != "confirmed")
            return Results.BadRequest(new { error = "Booking must be confirmed to start trip" });

        // Validate odometer
        if (request.Odometer <= 0)
            return Results.BadRequest(new { error = "Odometer reading must be greater than 0" });

        // Validate fuel level (0.0 to 1.0)
        if (request.FuelLevel < 0 || request.FuelLevel > 1.0)
            return Results.BadRequest(new { error = "Fuel level must be between 0 and 1.0" });

        // Check if trip already started
        if (booking.PreTripRecordedAt.HasValue)
            return Results.BadRequest(new { error = "Trip has already been started" });

        // Record pre-trip data
        booking.PreTripOdometer = request.Odometer;
        booking.PreTripFuelLevel = request.FuelLevel;
        booking.PreTripNotes = request.Notes;
        booking.PreTripPhotosJson = request.PhotoUrls != null && request.PhotoUrls.Any() 
            ? JsonSerializer.Serialize(request.PhotoUrls) 
            : null;
        booking.PreTripRecordedAt = DateTime.UtcNow;
        booking.PreTripRecordedBy = userId;
        booking.ActualPickupDateTime = DateTime.UtcNow;
        
        // Update pickupDateTime to actual pickup time
        booking.PickupDateTime = booking.ActualPickupDateTime.Value;

        // Update booking status to ongoing
        booking.Status = "ongoing";

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            bookingId = booking.Id,
            status = booking.Status,
            preTripOdometer = booking.PreTripOdometer,
            preTripFuelLevel = booking.PreTripFuelLevel,
            actualPickupDateTime = booking.ActualPickupDateTime,
            recordedBy = userId,
            recordedAt = booking.PreTripRecordedAt,
            message = "Trip started successfully"
        });
    }

    private static async Task<IResult> CompleteTripAsync(
        Guid bookingId,
        [FromBody] CompleteTripRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only owner or admin can complete trip
        if (!string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) && booking.OwnerId != userId)
            return Results.Forbid();

        // Validate booking status - must be ongoing
        if (booking.Status != "ongoing")
            return Results.BadRequest(new { error = "Booking must be ongoing to complete trip" });

        // Validate odometer
        if (request.Odometer <= 0)
            return Results.BadRequest(new { error = "Odometer reading must be greater than 0" });

        // Validate fuel level (0.0 to 1.0)
        if (request.FuelLevel < 0 || request.FuelLevel > 1.0)
            return Results.BadRequest(new { error = "Fuel level must be between 0 and 1.0" });

        // Check if pre-trip was recorded
        if (!booking.PreTripOdometer.HasValue)
            return Results.BadRequest(new { error = "Trip must be started before it can be completed" });

        // Validate odometer not going backwards
        if (request.Odometer < booking.PreTripOdometer.Value)
            return Results.BadRequest(new { error = $"Return odometer ({request.Odometer}) cannot be less than pickup odometer ({booking.PreTripOdometer.Value})" });

        // Check if trip already completed
        if (booking.PostTripRecordedAt.HasValue)
            return Results.BadRequest(new { error = "Trip has already been completed" });

        // Record post-trip data
        booking.PostTripOdometer = request.Odometer;
        booking.PostTripFuelLevel = request.FuelLevel;
        booking.PostTripNotes = request.Notes;
        booking.PostTripPhotosJson = request.PhotoUrls != null && request.PhotoUrls.Any()
            ? JsonSerializer.Serialize(request.PhotoUrls)
            : null;
        booking.PostTripRecordedAt = DateTime.UtcNow;
        booking.PostTripRecordedBy = userId;

        // Calculate prorated charges for early/late returns
        var actualPickup = booking.ActualPickupDateTime ?? booking.PickupDateTime;
        var actualReturn = booking.PostTripRecordedAt.Value;
        var originalPickup = booking.PickupDateTime;
        var originalReturn = booking.ReturnDateTime;

        // Calculate rental periods in days (minimum 1 day, rounded up)
        var bookedDays = Math.Max(1, (int)Math.Ceiling((originalReturn - originalPickup).TotalDays));
        var actualDays = Math.Max(1, (int)Math.Ceiling((actualReturn - actualPickup).TotalDays));

        // Calculate per-day rates
        var dailyRentalRate = booking.RentalAmount / bookedDays;
        var dailyDriverRate = booking.WithDriver && booking.DriverAmount.HasValue 
            ? booking.DriverAmount.Value / bookedDays 
            : 0;
        var dailyProtectionRate = booking.ProtectionAmount.HasValue 
            ? booking.ProtectionAmount.Value / bookedDays 
            : 0;

        // Calculate prorated amounts based on actual days
        var proratedRentalAmount = dailyRentalRate * actualDays;
        var proratedDriverAmount = dailyDriverRate * actualDays;
        var proratedProtectionAmount = dailyProtectionRate * actualDays;

        // Platform fee is typically % of rental, so prorate it too
        var proratedPlatformFee = booking.PlatformFee.HasValue 
            ? (booking.PlatformFee.Value / bookedDays) * actualDays 
            : 0;

        // Insurance is typically fixed, don't prorate
        var proratedInsuranceAmount = booking.InsuranceAmount ?? 0;

        // Calculate new total (excluding deposit and promo - those don't change)
        var proratedTotal = proratedRentalAmount + proratedDriverAmount + proratedProtectionAmount + 
                           proratedInsuranceAmount + proratedPlatformFee;

        // Calculate refund/charge amount
        var originalChargeableAmount = booking.TotalAmount; // This already excludes deposit
        var adjustmentAmount = originalChargeableAmount - proratedTotal;

        string? adjustmentType = null;
        PaymentTransaction? adjustmentTransaction = null;

        // Early return - issue refund
        if (adjustmentAmount > 0.01m) // Early return, refund unused days
        {
            adjustmentType = "early_return_refund";
            
            // Update booking amounts to reflect actual usage
            booking.RentalAmount = proratedRentalAmount;
            if (booking.WithDriver && booking.DriverAmount.HasValue)
                booking.DriverAmount = proratedDriverAmount;
            if (booking.ProtectionAmount.HasValue)
                booking.ProtectionAmount = proratedProtectionAmount;
            if (booking.PlatformFee.HasValue)
                booking.PlatformFee = proratedPlatformFee;
            booking.TotalAmount = proratedTotal;

            // Create refund transaction
            adjustmentTransaction = new PaymentTransaction
            {
                BookingId = booking.Id,
                UserId = booking.RenterId,
                Type = "refund",
                Status = "pending",
                Amount = adjustmentAmount,
                Currency = booking.Currency,
                Method = booking.PaymentMethod,
                Reference = $"REFUND-EARLY-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    reason = "Early return - prorated refund",
                    bookedDays,
                    actualDays,
                    originalAmount = originalChargeableAmount,
                    proratedAmount = proratedTotal,
                    refundAmount = adjustmentAmount,
                    dailyRentalRate,
                    dailyDriverRate,
                    dailyProtectionRate
                }),
                CreatedAt = DateTime.UtcNow
            };

            db.PaymentTransactions.Add(adjustmentTransaction);
            booking.PaymentStatus = "partial_refund";
        }
        // Late return - create additional charge
        else if (adjustmentAmount < -0.01m) // Late return, charge extra days
        {
            adjustmentType = "late_return_charge";
            var extraCharge = Math.Abs(adjustmentAmount);

            // Update booking amounts to reflect extended usage
            booking.RentalAmount = proratedRentalAmount;
            if (booking.WithDriver && booking.DriverAmount.HasValue)
                booking.DriverAmount = proratedDriverAmount;
            if (booking.ProtectionAmount.HasValue)
                booking.ProtectionAmount = proratedProtectionAmount;
            if (booking.PlatformFee.HasValue)
                booking.PlatformFee = proratedPlatformFee;
            booking.TotalAmount = proratedTotal;

            // Create charge transaction
            adjustmentTransaction = new PaymentTransaction
            {
                BookingId = booking.Id,
                UserId = booking.RenterId,
                Type = "payment",
                Status = "pending",
                Amount = extraCharge,
                Currency = booking.Currency,
                Method = booking.PaymentMethod,
                Reference = $"CHARGE-LATE-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    reason = "Late return - additional charges",
                    bookedDays,
                    actualDays,
                    originalAmount = originalChargeableAmount,
                    proratedAmount = proratedTotal,
                    extraCharge,
                    dailyRentalRate,
                    dailyDriverRate,
                    dailyProtectionRate
                }),
                CreatedAt = DateTime.UtcNow
            };

            db.PaymentTransactions.Add(adjustmentTransaction);
        }

        // ===== AUTOMATIC MILEAGE OVERAGE CALCULATION =====
        decimal mileageOverageCharge = 0m;
        int? actualKmDriven = null;
        int? allowedKm = null;
        int? overageKm = null;
        decimal pricePerKm = 0m;
        
        if (booking.PreTripOdometer.HasValue && booking.PostTripOdometer.HasValue && booking.Vehicle != null)
        {
            actualKmDriven = booking.PostTripOdometer.Value - booking.PreTripOdometer.Value;
            
            // Try to get mileage configuration from InclusionsJson first, then fall back to database columns
            int includedKm = 0;
            if (!string.IsNullOrWhiteSpace(booking.Vehicle.InclusionsJson))
            {
                try
                {
                    var inclusions = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(booking.Vehicle.InclusionsJson);
                    if (inclusions != null)
                    {
                        if (inclusions.TryGetValue("mileageAllowancePerDay", out var mileageAllowance))
                            includedKm = mileageAllowance.GetInt32() * actualDays;
                        
                        if (inclusions.TryGetValue("extraKmRate", out var kmRate))
                            pricePerKm = kmRate.GetDecimal();
                    }
                }
                catch { /* Fall back to database columns */ }
            }
            
            // Fall back to database columns if not found in JSON
            if (includedKm == 0 && booking.Vehicle.IncludedKilometers > 0)
                includedKm = booking.Vehicle.IncludedKilometers;
            if (pricePerKm == 0 && booking.Vehicle.PricePerExtraKm > 0)
                pricePerKm = booking.Vehicle.PricePerExtraKm;
            
            // Check if vehicle has mileage charging enabled and configured
            if (booking.Vehicle.MileageChargingEnabled && includedKm > 0 && pricePerKm > 0)
            {
                allowedKm = includedKm;
                
                // Calculate overage
                if (actualKmDriven > allowedKm)
                {
                    overageKm = actualKmDriven.Value - allowedKm.Value;
                    mileageOverageCharge = overageKm.Value * pricePerKm;
                    
                    // Get or create mileage overage charge type
                    var mileageChargeType = await db.PostRentalChargeTypes
                        .FirstOrDefaultAsync(ct => ct.Code == "mileage_overage");
                    
                    if (mileageChargeType == null)
                    {
                        mileageChargeType = new PostRentalChargeType
                        {
                            Code = "mileage_overage",
                            Name = "Mileage Overage",
                            Description = "Charge for exceeding included kilometers",
                            DefaultAmount = 0,
                            Currency = "GHS",
                            RecipientType = "owner",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.PostRentalChargeTypes.Add(mileageChargeType);
                        await db.SaveChangesAsync(); // Save to get ID
                    }
                    
                    // Create automatic mileage charge
                    var mileageCharge = new BookingCharge
                    {
                        BookingId = booking.Id,
                        ChargeTypeId = mileageChargeType.Id,
                        Amount = mileageOverageCharge,
                        Currency = booking.Currency,
                        Label = $"Mileage Overage ({overageKm} km)",
                        Notes = $"Automatic charge: {actualKmDriven} km driven, {allowedKm} km included, {overageKm} km overage @ {pricePerKm:F2}/km",
                        Status = "approved", // Auto-approved since it's system-generated
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        SettledAt = DateTime.UtcNow,
                        EvidencePhotoUrlsJson = booking.PostTripPhotosJson ?? "[]"
                    };
                    
                    db.BookingCharges.Add(mileageCharge);
                    
                    // Calculate platform fee on overage (typically 15%)
                    decimal platformFeePercentage = 0.15m; // Default 15%
                    var platformFeeSetting = await db.GlobalSettings
                        .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
                    if (platformFeeSetting != null)
                    {
                        try
                        {
                            var feeValue = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson);
                            platformFeePercentage = feeValue / 100m;
                        }
                        catch
                        {
                            // Use default if parsing fails
                        }
                    }
                    
                    decimal overagePlatformFee = mileageOverageCharge * platformFeePercentage;
                    
                    // Update booking totals to include overage
                    booking.RentalAmount += mileageOverageCharge;
                    booking.PlatformFee = (booking.PlatformFee ?? 0) + overagePlatformFee;
                    booking.TotalAmount += mileageOverageCharge;
                    
                    // Deduct overage charge from deposit
                    if (booking.DepositAmount >= mileageOverageCharge)
                    {
                        booking.DepositAmount -= mileageOverageCharge;
                    }
                    else if (booking.DepositAmount > 0)
                    {
                        // Partial deduction - charge remaining to renter
                        var remainingCharge = mileageOverageCharge - booking.DepositAmount;
                        booking.DepositAmount = 0;
                        
                        // Create payment transaction for remaining balance
                        var balanceTransaction = new PaymentTransaction
                        {
                            BookingId = booking.Id,
                            UserId = booking.RenterId,
                            Type = "payment",
                            Status = "pending",
                            Amount = remainingCharge,
                            Currency = booking.Currency,
                            Method = booking.PaymentMethod,
                            Reference = $"MILEAGE-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                            MetadataJson = JsonSerializer.Serialize(new
                            {
                                reason = "Mileage overage charge (deposit insufficient)",
                                actualKm = actualKmDriven,
                                includedKm = allowedKm,
                                overageKm,
                                ratePerKm = pricePerKm,
                                totalOverageCharge = mileageOverageCharge,
                                depositUsed = booking.DepositAmount,
                                remainingBalance = remainingCharge
                            }),
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        db.PaymentTransactions.Add(balanceTransaction);
                    }
                    else
                    {
                        // No deposit - full charge to renter
                        var chargeTransaction = new PaymentTransaction
                        {
                            BookingId = booking.Id,
                            UserId = booking.RenterId,
                            Type = "payment",
                            Status = "pending",
                            Amount = mileageOverageCharge,
                            Currency = booking.Currency,
                            Method = booking.PaymentMethod,
                            Reference = $"MILEAGE-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                            MetadataJson = JsonSerializer.Serialize(new
                            {
                                reason = "Mileage overage charge",
                                actualKm = actualKmDriven,
                                includedKm = allowedKm,
                                overageKm,
                                ratePerKm = pricePerKm,
                                totalCharge = mileageOverageCharge
                            }),
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        db.PaymentTransactions.Add(chargeTransaction);
                    }
                }
            }
        }

        // Update ReturnDateTime to actual return time
        // Ensure returnDateTime is never before pickupDateTime to prevent negative durations
        if (actualReturn < booking.PickupDateTime)
        {
            booking.ReturnDateTime = booking.PickupDateTime.AddHours(1); // Minimum 1 hour rental
        }
        else
        {
            booking.ReturnDateTime = actualReturn;
        }

        // Update booking status to completed
        booking.Status = "completed";
        booking.UpdatedAt = DateTime.UtcNow;
        
        // Cancel any pending return reminder notifications (trip already completed)
        var pendingReturnReminders = await db.NotificationJobs
            .Where(j => j.Status == "pending" && 
                       j.TemplateName == "return_reminder" &&
                       j.MetadataJson.Contains(booking.Id.ToString()))
            .ToListAsync();
        
        foreach (var reminder in pendingReturnReminders)
        {
            reminder.Status = "cancelled";
            reminder.UpdatedAt = DateTime.UtcNow;
        }
        
        // Send booking completed notifications
        try
        {
            var notificationService = context.RequestServices.GetRequiredService<INotificationService>();
            await notificationService.SendBookingCompletedNotificationAsync(booking);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending booking completed notification: {ex.Message}");
        }

        // Auto-create deposit refund (with reduced amount if mileage charges were deducted)
        var existingRefund = await db.DepositRefunds.FirstOrDefaultAsync(r => r.BookingId == bookingId);
        
        if (existingRefund == null && booking.DepositAmount > 0)
        {
            var refundNotes = "Auto-created deposit refund after trip completion";
            if (mileageOverageCharge > 0)
            {
                refundNotes += $"\nMileage overage charge of {booking.Currency} {mileageOverageCharge:F2} deducted from deposit";
            }
            
            var refund = new DepositRefund
            {
                BookingId = bookingId,
                Amount = booking.DepositAmount,
                Currency = booking.Currency,
                PaymentMethod = booking.PaymentMethod,
                Status = "pending",
                DueDate = DateTime.UtcNow.AddDays(2),
                Reference = $"REF-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Notes = refundNotes
            };

            db.DepositRefunds.Add(refund);

            var auditLog = new RefundAuditLog
            {
                DepositRefundId = refund.Id,
                Action = "created",
                OldStatus = "",
                NewStatus = "pending",
                PerformedByUserId = userId,
                Notes = "Auto-created when trip was completed"
            };

            db.RefundAuditLogs.Add(auditLog);
        }

        await db.SaveChangesAsync();

        var distanceTraveled = request.Odometer - booking.PreTripOdometer.Value;
        var tripDuration = booking.PostTripRecordedAt.Value - (booking.ActualPickupDateTime ?? booking.PickupDateTime);

        return Results.Ok(new
        {
            bookingId = booking.Id,
            status = booking.Status,
            preTripOdometer = booking.PreTripOdometer,
            postTripOdometer = booking.PostTripOdometer,
            distanceTraveled,
            preTripFuelLevel = booking.PreTripFuelLevel,
            postTripFuelLevel = booking.PostTripFuelLevel,
            actualPickupDateTime = booking.ActualPickupDateTime,
            completedAt = booking.PostTripRecordedAt,
            tripDuration = new
            {
                days = tripDuration.Days,
                hours = tripDuration.Hours,
                minutes = tripDuration.Minutes,
                totalHours = tripDuration.TotalHours
            },
            rentalPeriod = new
            {
                bookedDays,
                actualDays,
                earlyReturnDays = bookedDays > actualDays ? bookedDays - actualDays : 0,
                lateReturnDays = actualDays > bookedDays ? actualDays - bookedDays : 0
            },
            charges = new
            {
                originalRentalAmount = dailyRentalRate * bookedDays,
                proratedRentalAmount,
                originalDriverAmount = dailyDriverRate * bookedDays,
                proratedDriverAmount,
                originalProtectionAmount = dailyProtectionRate * bookedDays,
                proratedProtectionAmount,
                originalTotal = originalChargeableAmount,
                proratedTotal,
                adjustmentAmount,
                adjustmentType
            },
            mileageCharge = mileageOverageCharge > 0 ? new
            {
                amount = mileageOverageCharge,
                actualKmDriven,
                allowedKm,
                overageKm,
                ratePerKm = booking.Vehicle?.PricePerExtraKm,
                deductedFromDeposit = mileageOverageCharge,
                remainingDeposit = booking.DepositAmount,
                message = $"Mileage overage charge of {booking.Currency} {mileageOverageCharge:F2} applied ({overageKm} km @ {booking.Vehicle?.PricePerExtraKm:F2}/km). Deducted from security deposit."
            } : null,
            updatedBookingTotals = new
            {
                rentalAmount = booking.RentalAmount,
                platformFee = booking.PlatformFee,
                totalAmount = booking.TotalAmount,
                depositAmount = booking.DepositAmount
            },
            adjustment = adjustmentTransaction != null ? new
            {
                transactionId = adjustmentTransaction.Id,
                type = adjustmentTransaction.Type,
                amount = adjustmentTransaction.Amount,
                status = adjustmentTransaction.Status,
                reference = adjustmentTransaction.Reference,
                message = adjustmentType == "early_return_refund" 
                    ? $"Refund of {adjustmentTransaction.Amount:F2} {booking.Currency} will be processed for {bookedDays - actualDays} unused day(s)"
                    : $"Additional charge of {adjustmentTransaction.Amount:F2} {booking.Currency} pending for {actualDays - bookedDays} extra day(s)"
            } : null,
            recordedBy = userId,
            message = mileageOverageCharge > 0 
                ? $"Trip completed. Mileage overage charge of {booking.Currency} {mileageOverageCharge:F2} applied."
                : "Trip completed successfully"
        });
    }

    private static async Task<IResult> ResendConfirmationEmailAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        INotificationService notificationService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Results.Unauthorized();

        // Get booking with all related data
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Owner)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
            .Include(b => b.PickupInspection)
            .Include(b => b.ReturnInspection)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        // Authorization: Owner of the vehicle, admin, or the renter can resend
        var isOwner = booking.Vehicle?.OwnerId == userId;
        var isAdmin = user.Role == "admin";
        var isRenter = booking.RenterId == userId;

        if (!isOwner && !isAdmin && !isRenter)
            return Results.Forbid();

        // Verify booking has a confirmed payment or is confirmed
        if (booking.Status != "confirmed" && booking.Status != "active" && booking.Status != "completed")
        {
            return Results.BadRequest(new { error = "Can only resend confirmation for confirmed, active, or completed bookings" });
        }

        try
        {
            // Resend confirmation email to customer
            await notificationService.SendBookingConfirmationToCustomerAsync(booking);

            _logger?.LogInformation("Resent booking confirmation email for booking {BookingId} by user {UserId}", 
                bookingId, userId);

            return Results.Ok(new 
            { 
                success = true, 
                message = "Confirmation email has been resent",
                bookingReference = booking.BookingReference,
                sentTo = booking.Renter?.Email ?? booking.GuestEmail
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to resend confirmation email for booking {BookingId}", bookingId);
            return Results.Problem(
                detail: "Failed to send confirmation email. Please try again later.",
                statusCode: 500
            );
        }
    }

    private static async Task<IResult> UpdateGuestContactAsync(
        string bookingReference,
        [FromBody] UpdateGuestContactRequest request,
        AppDbContext db,
        IEmailService emailService)
    {
        // Find booking by reference
        var booking = await db.Bookings
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);
        
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });
        
        // Only allow updates before pickup or during active rental
        if (booking.Status != "pending_payment" && booking.Status != "confirmed" && booking.Status != "active")
            return Results.BadRequest(new { error = "Contact information can only be updated before pickup or during active rental" });
        
        var oldEmail = booking.GuestEmail;
        
        // Update guest contact info
        if (!string.IsNullOrWhiteSpace(request.NewEmail))
            booking.GuestEmail = request.NewEmail;
        
        if (!string.IsNullOrWhiteSpace(request.NewPhone))
            booking.GuestPhone = NormalizePhoneNumber(request.NewPhone);
        
        if (!string.IsNullOrWhiteSpace(request.NewFirstName))
            booking.GuestFirstName = request.NewFirstName;
            
        if (!string.IsNullOrWhiteSpace(request.NewLastName))
            booking.GuestLastName = request.NewLastName;
        
        booking.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
        
        // Send confirmation to new email only
        if (!string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            var message = $@"
                <h2>Booking Contact Information Updated</h2>
                <p>Your contact information for booking <strong>{bookingReference}</strong> has been successfully updated.</p>
                <p><strong>Updated Details:</strong></p>
                <ul>
                    <li>Email: {booking.GuestEmail}</li>
                    <li>Phone: {booking.GuestPhone}</li>
                    <li>Name: {booking.GuestFirstName} {booking.GuestLastName}</li>
                </ul>
                <p>If you did not make this change, please contact us immediately.</p>
            ";
            
            try
            {
                await emailService.SendEmailAsync(booking.GuestEmail, "Booking Contact Information Updated", message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send contact update confirmation email for booking {Reference}", bookingReference);
                // Don't fail the update if email fails
            }
        }
        
        return Results.Ok(new
        {
            message = "Contact information updated successfully",
            booking = new
            {
                bookingReference = booking.BookingReference,
                email = booking.GuestEmail,
                phone = booking.GuestPhone,
                firstName = booking.GuestFirstName,
                lastName = booking.GuestLastName
            }
        });
    }

    private static async Task<IResult> UpdateGuestContactByStaffAsync(
        Guid bookingId,
        [FromBody] UpdateGuestContactRequest request,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();
        
        // Only owner/admin can update
        if (user.Role != "owner" && user.Role != "admin")
            return Results.Forbid();
        
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
            
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });
        
        // Owner can only update their own bookings
        if (user.Role == "owner" && booking.OwnerId != userId)
            return Results.Forbid();
        
        // Update contact info
        if (!string.IsNullOrWhiteSpace(request.NewEmail))
            booking.GuestEmail = request.NewEmail;
        
        if (!string.IsNullOrWhiteSpace(request.NewPhone))
            booking.GuestPhone = NormalizePhoneNumber(request.NewPhone);
        
        if (!string.IsNullOrWhiteSpace(request.NewFirstName))
            booking.GuestFirstName = request.NewFirstName;
            
        if (!string.IsNullOrWhiteSpace(request.NewLastName))
            booking.GuestLastName = request.NewLastName;
        
        booking.UpdatedAt = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
        
        return Results.Ok(new
        {
            message = "Guest contact updated successfully",
            booking = new
            {
                bookingReference = booking.BookingReference,
                email = booking.GuestEmail,
                phone = booking.GuestPhone,
                firstName = booking.GuestFirstName,
                lastName = booking.GuestLastName
            }
        });
    }

    private static async Task<IResult> GetBookingByReferenceAsync(
        string bookingReference,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.City)
            .Include(b => b.Driver)
            .Include(b => b.ProtectionPlan)
            .Include(b => b.InsurancePlan)
            .Include(b => b.PromoCode)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Authorization check: Allow admins, renters (own booking), and guests (anonymous)
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                // Authenticated user - check if admin or owns the booking
                var isAdmin = user.Role == "admin";
                var isRenter = booking.RenterId == userId;
                var isOwner = booking.OwnerId == userId;
                
                if (!isAdmin && !isRenter && !isOwner)
                {
                    return Results.Forbid();
                }
            }
        }
        // If not authenticated, allow access (guest booking)

        // Get owner information
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == booking.OwnerId);

        // Format pickup location from JSON
        var pickupLocation = "TBD";
        if (!string.IsNullOrWhiteSpace(booking.PickupLocationJson))
        {
            try
            {
                var locationObj = JsonSerializer.Deserialize<JsonElement>(booking.PickupLocationJson);
                if (locationObj.TryGetProperty("cityName", out var cityName))
                {
                    pickupLocation = cityName.GetString() ?? "TBD";
                }
                else if (locationObj.TryGetProperty("address", out var address))
                {
                    pickupLocation = address.GetString() ?? "TBD";
                }
            }
            catch
            {
                pickupLocation = booking.PickupLocationJson;
            }
        }

        var returnLocation = "TBD";
        if (!string.IsNullOrWhiteSpace(booking.ReturnLocationJson))
        {
            try
            {
                var locationObj = JsonSerializer.Deserialize<JsonElement>(booking.ReturnLocationJson);
                if (locationObj.TryGetProperty("cityName", out var cityName))
                {
                    returnLocation = cityName.GetString() ?? "TBD";
                }
                else if (locationObj.TryGetProperty("address", out var address))
                {
                    returnLocation = address.GetString() ?? "TBD";
                }
            }
            catch
            {
                returnLocation = booking.ReturnLocationJson;
            }
        }

        // Parse vehicle photos
        var photos = new List<string>();
        if (!string.IsNullOrWhiteSpace(booking.Vehicle?.PhotosJson))
        {
            try
            {
                photos = JsonSerializer.Deserialize<List<string>>(booking.Vehicle.PhotosJson) ?? new List<string>();
            }
            catch { }
        }

        return Results.Ok(new
        {
            id = booking.Id,
            bookingReference = booking.BookingReference,
            status = booking.Status,
            paymentStatus = booking.PaymentStatus,
            pickupDateTime = booking.PickupDateTime,
            returnDateTime = booking.ReturnDateTime,
            pickupLocation = pickupLocation,
            returnLocation = returnLocation,
            withDriver = booking.WithDriver,
            currency = booking.Currency,
            rentalAmount = booking.RentalAmount,
            depositAmount = booking.DepositAmount,
            driverAmount = booking.DriverAmount,
            insuranceAmount = booking.InsuranceAmount,
            protectionAmount = booking.ProtectionAmount,
            platformFee = booking.PlatformFee,
            promoDiscountAmount = booking.PromoDiscountAmount,
            totalAmount = booking.TotalAmount,
            paymentMethod = booking.PaymentMethod,
            createdAt = booking.CreatedAt,
            agreementSigned = booking.AgreementSigned,
            agreementSignedAt = booking.AgreementSignedAt,
            vehicle = booking.Vehicle == null ? null : new
            {
                id = booking.Vehicle.Id,
                make = booking.Vehicle.Make,
                model = booking.Vehicle.Model,
                year = booking.Vehicle.Year,
                plateNumber = booking.Vehicle.PlateNumber,
                transmission = booking.Vehicle.Transmission,
                fuelType = booking.Vehicle.FuelType,
                seatingCapacity = booking.Vehicle.SeatingCapacity,
                hasAC = booking.Vehicle.HasAC,
                dailyRate = booking.Vehicle.DailyRate,
                photos = photos,
                category = booking.Vehicle.Category == null ? null : new
                {
                    name = booking.Vehicle.Category.Name,
                    defaultDailyRate = booking.Vehicle.Category.DefaultDailyRate,
                    defaultDepositAmount = booking.Vehicle.Category.DefaultDepositAmount
                },
                city = booking.Vehicle.City == null ? null : new
                {
                    name = booking.Vehicle.City.Name
                }
            },
            renter = booking.Renter == null ? null : new
            {
                firstName = booking.Renter.FirstName,
                lastName = booking.Renter.LastName,
                email = booking.Renter.Email,
                phone = booking.Renter.Phone
            },
            guestEmail = booking.GuestEmail,
            guestPhone = booking.GuestPhone,
            guestFirstName = booking.GuestFirstName,
            guestLastName = booking.GuestLastName,
            driver = booking.Driver == null ? null : new
            {
                firstName = booking.Driver.FirstName,
                lastName = booking.Driver.LastName,
                phone = booking.Driver.Phone
            },
            owner = owner == null ? null : new
            {
                firstName = owner.FirstName,
                lastName = owner.LastName,
                email = owner.Email,
                phone = owner.Phone,
                businessPhone = owner.OwnerProfile?.BusinessPhone,
                businessAddress = owner.OwnerProfile?.BusinessAddress,
                gpsAddress = owner.OwnerProfile?.GpsAddress,
                pickupInstructions = owner.OwnerProfile?.PickupInstructions
            },
            protectionPlan = booking.ProtectionPlan == null ? null : new
            {
                name = booking.ProtectionPlan.Name,
                code = booking.ProtectionPlan.Code,
                dailyPrice = booking.ProtectionPlan.DailyPrice,
                fixedPrice = booking.ProtectionPlan.FixedPrice
            },
            insurancePlan = booking.InsurancePlan == null ? null : new
            {
                name = booking.InsurancePlan.Name,
                dailyPrice = booking.InsurancePlan.DailyPrice
            },
            promoCode = booking.PromoCode == null ? null : new
            {
                code = booking.PromoCode.Code,
                promoType = booking.PromoCode.PromoType,
                discountValue = booking.PromoCode.DiscountValue
            }
        });
    }

    private static async Task<IResult> SignAgreementAsync(
        Guid bookingId,
        [FromBody] SignAgreementRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        AppDbContext db)
    {
        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Authorization check: Allow admins, renters (own booking), and guests (anonymous)
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                // Authenticated user - check if admin or owns the booking
                var isAdmin = user.Role == "admin";
                var isRenter = booking.RenterId == userId;
                
                if (!isAdmin && !isRenter)
                {
                    return Results.Forbid();
                }
            }
        }
        // If not authenticated, allow access (guest booking)

        // Only allow agreement signing for unpaid bookings
        if (booking.PaymentStatus != "unpaid")
            return Results.BadRequest(new { error = "Agreement can only be signed for unpaid bookings" });

        if (booking.AgreementSigned)
            return Results.BadRequest(new { error = "Agreement has already been signed" });

        // Get IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Update booking with agreement signature
        booking.AgreementSigned = true;
        booking.AgreementSignedAt = DateTime.UtcNow;
        booking.AgreementSignedBy = request.SignedByName;
        booking.AgreementIpAddress = ipAddress;
        booking.AgreementSignatureData = request.SignatureData; // Can be base64 image or acceptance token
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Agreement signed successfully",
            bookingReference = booking.BookingReference,
            agreementSigned = true,
            agreementSignedAt = booking.AgreementSignedAt,
            signedBy = booking.AgreementSignedBy
        });
    }

    private static async Task<IResult> InitiatePaymentAsync(
        Guid bookingId,
        [FromBody] InitiatePaymentRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IPaystackPaymentService paystackService,
        IStripePaymentService stripeService,
        IAppConfigService configService)
    {
        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Authorization check: Allow admins, renters (own booking), and guests (anonymous)
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                // Authenticated user - check if admin or owns the booking
                var isAdmin = user.Role == "admin";
                var isRenter = booking.RenterId == userId;
                
                if (!isAdmin && !isRenter)
                {
                    return Results.Forbid();
                }
            }
        }
        // If not authenticated, allow access (guest booking)

        // Only allow payment initiation for unpaid bookings
        if (booking.PaymentStatus != "unpaid")
            return Results.BadRequest(new { error = "Payment can only be initiated for unpaid bookings" });

        // Check if booking is not cancelled
        if (booking.Status == "cancelled")
            return Results.BadRequest(new { error = "Cannot initiate payment for cancelled booking" });

        // Optionally require agreement to be signed first
        var requireAgreement = await configService.GetConfigValueAsync("Payment:RequireAgreementBeforePayment");
        if (requireAgreement?.ToLower() == "true" && !booking.AgreementSigned)
        {
            return Results.BadRequest(new { error = "Rental agreement must be signed before initiating payment" });
        }

        // Determine email for payment
        var email = request.Email ?? booking.Renter?.Email ?? booking.GuestEmail;
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest(new { error = "Email is required for payment" });

        // Generate payment reference
        var paymentReference = $"PAY-{booking.BookingReference}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Create payment transaction record
        var transaction = new PaymentTransaction
        {
            UserId = booking.RenterId,
            BookingId = booking.Id,
            Type = "payment",
            Status = "pending",
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            Method = request.PaymentMethod?.ToLower() ?? "momo",
            Reference = paymentReference,
            CreatedAt = DateTime.UtcNow
        };

        db.PaymentTransactions.Add(transaction);
        await db.SaveChangesAsync();

        try
        {
            // Determine payment provider based on method
            var paymentMethod = request.PaymentMethod?.ToLower() ?? "momo";
            
            if (paymentMethod == "card")
            {
                // Use Stripe for card payments
                try
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "booking_id", booking.Id.ToString() },
                        { "booking_reference", booking.BookingReference },
                        { "reference", paymentReference }
                    };

                    // Create Stripe customer if needed
                    var customerName = booking.Renter != null 
                        ? $"{booking.Renter.FirstName} {booking.Renter.LastName}".Trim()
                        : $"{booking.GuestFirstName} {booking.GuestLastName}".Trim();
                    
                    var customerId = await stripeService.CreateCustomerAsync(email, customerName, booking.Renter?.Phone ?? booking.GuestPhone ?? "");

                    var paymentIntent = await stripeService.CreatePaymentIntentAsync(
                        booking.TotalAmount,
                        booking.Currency,
                        customerId,
                        metadata
                    );

                    transaction.ExternalTransactionId = paymentIntent.Id;
                    await db.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        paymentProvider = "stripe",
                        paymentReference = paymentReference,
                        clientSecret = paymentIntent.ClientSecret,
                        paymentIntentId = paymentIntent.Id,
                        amount = booking.TotalAmount,
                        currency = booking.Currency,
                        bookingReference = booking.BookingReference
                    });
                }
                catch (PaymentProviderNotConfiguredException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            }
            else
            {
                // Use Paystack for mobile money and other methods
                try
                {
                    var metadata = new Dictionary<string, string>
                    {
                        { "booking_id", booking.Id.ToString() },
                        { "booking_reference", booking.BookingReference },
                        { "payment_method", paymentMethod }
                    };

                    var result = await paystackService.InitializeTransactionAsync(
                        booking.TotalAmount,
                        email,
                        paymentReference,
                        metadata
                    );

                    transaction.ExternalTransactionId = result.Reference;
                    await db.SaveChangesAsync();

                    return Results.Ok(new
                    {
                        paymentProvider = "paystack",
                        paymentReference = result.Reference,
                        authorizationUrl = result.AuthorizationUrl,
                        amount = booking.TotalAmount,
                        currency = booking.Currency,
                        bookingReference = booking.BookingReference
                    });
                }
                catch (PaymentProviderNotConfiguredException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            }
        }
        catch (Exception ex)
        {
            transaction.Status = "failed";
            await db.SaveChangesAsync();
            
            return Results.BadRequest(new { error = $"Payment initiation failed: {ex.Message}" });
        }
    }
}

public record UpdateGuestContactRequest(
    string? NewEmail,
    string? NewPhone,
    string? NewFirstName,
    string? NewLastName
);

public record SignAgreementRequest(
    string SignedByName,
    string? SignatureData
);

public record InitiatePaymentRequest(
    string? Email,
    string? PaymentMethod
);
