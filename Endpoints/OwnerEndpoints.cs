using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Services;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Extensions; // Absolutize URL helper

namespace GhanaHybridRentalApi.Endpoints;

public static class OwnerEndpoints
{
    // Helper method to sync mileage fields into inclusions object
    private static string? GetSyncedInclusionsJson(
        Dictionary<string, object>? inclusions,
        decimal? mileageAllowancePerDay,
        decimal? extraKmRate)
    {
        if (inclusions is null && !mileageAllowancePerDay.HasValue && !extraKmRate.HasValue)
            return null;

        // Start with provided inclusions or create new dictionary
        var syncedInclusions = inclusions is not null 
            ? new Dictionary<string, object>(inclusions) 
            : new Dictionary<string, object>();

        // Sync mileage values from top-level fields into inclusions
        if (mileageAllowancePerDay.HasValue)
            syncedInclusions["mileageAllowancePerDay"] = mileageAllowancePerDay.Value;

        if (extraKmRate.HasValue)
            syncedInclusions["extraKmRate"] = extraKmRate.Value;

        return JsonSerializer.Serialize(syncedInclusions);
    }

    public static void MapOwnerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/owner/vehicles", GetOwnerVehiclesAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapPost("/api/v1/owner/vehicles", CreateVehicleAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapPut("/api/v1/owner/vehicles/{vehicleId:guid}", UpdateVehicleAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapDelete("/api/v1/owner/vehicles/{vehicleId:guid}", DeleteVehicleAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapDelete("/api/v1/owner/vehicles/{vehicleId:guid}/force", ForceDeleteVehicleAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/bookings", GetOwnerBookingsAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/drivers", GetOwnerDriversAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/earnings", GetOwnerEarningsAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/payouts", GetOwnerPayoutsAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapPost("/api/v1/owner/vehicles/{vehicleId:guid}/images", UploadVehicleImagesAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/payout-details", GetPayoutDetailsAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapPut("/api/v1/owner/payout-details", UpdatePayoutDetailsAsync)
            .RequireAuthorization("OwnerOnly");

        // Mileage settings endpoint for owners to get validation constraints
        app.MapGet("/api/v1/owner/settings/mileage", GetOwnerMileageSettingsAsync)
            .RequireAuthorization("OwnerOnly");

        // Vehicle data lookup endpoint - auto-populate vehicle details from year/make/model
        app.MapGet("/api/v1/owner/vehicles/lookup/{year:int}/{make}/{model}", LookupVehicleDataAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owners/me/verification-status", GetVerificationStatusAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapGet("/api/v1/owner/profile", GetOwnerProfileAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapPut("/api/v1/owner/profile", UpdateOwnerProfileAsync)
            .RequireAuthorization("OwnerOnly");

        app.MapPost("/api/v1/owner/payouts/request", RequestPayoutAsync)
            .RequireAuthorization("OwnerOnly");

        // Owner notifications endpoints
        app.MapGet("/api/v1/owner/notifications", GetOwnerNotificationsAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapPut("/api/v1/owner/notifications/{id:guid}/read", MarkOwnerNotificationReadAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapDelete("/api/v1/owner/notifications/{id:guid}", DeleteOwnerNotificationAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapGet("/api/v1/owner/notification-preferences", GetOwnerNotificationPreferencesAsync)
            .RequireAuthorization("OwnerOnly");
        app.MapPut("/api/v1/owner/notification-preferences", UpdateOwnerNotificationPreferencesAsync)
            .RequireAuthorization("OwnerOnly");

        // Owner rental agreements - view agreements signed by renters for their vehicles
        app.MapGet("/api/v1/owner/bookings/{bookingId:guid}/rental-agreement", GetOwnerBookingAgreementAsync)
            .RequireAuthorization("OwnerOnly");

    }

    private static async Task<IResult> GetOwnerNotificationsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Notifications.Where(n => n.UserId == userId);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = items });
    }

    private static async Task<IResult> MarkOwnerNotificationReadAsync(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return Results.NotFound(new { error = "Notification not found" });

        n.Read = true;
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> DeleteOwnerNotificationAsync(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return Results.NotFound(new { error = "Notification not found" });

        db.Notifications.Remove(n);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Notification deleted" });
    }

    private static async Task<IResult> GetOwnerNotificationPreferencesAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.OwnerProfile == null)
            return Results.NotFound(new { error = "Owner profile not found" });

        // Parse existing preferences or return defaults
        var preferences = new
        {
            emailNotifications = true,
            smsNotifications = false,
            newBooking = true,
            bookingConfirmed = true,
            paymentReceived = true,
            payoutRequest = true,
            newReview = true,
            reportFiled = true
        };

        if (!string.IsNullOrWhiteSpace(user.OwnerProfile.NotificationPreferencesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, bool>>(user.OwnerProfile.NotificationPreferencesJson);
                if (parsed != null)
                {
                    return Results.Ok(parsed);
                }
            }
            catch { }
        }

        return Results.Ok(preferences);
    }

    private static async Task<IResult> UpdateOwnerNotificationPreferencesAsync(
        [FromBody] Dictionary<string, bool> preferences,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.OwnerProfile == null)
            return Results.NotFound(new { error = "Owner profile not found" });

        user.OwnerProfile.NotificationPreferencesJson = JsonSerializer.Serialize(preferences);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, preferences });
    }


    private static async Task<IResult> GetOwnerVehiclesAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var vehicles = await db.Vehicles
            .Where(v => v.OwnerId == userId)
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            total = vehicles.Count,
            hasResults = vehicles.Any(),
            message = vehicles.Any() ? null : "You haven't added any vehicles yet. Add your first vehicle to start earning.",
            data = vehicles
        });
    }

    public class CreateVehicleRequest
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? CityId { get; set; } // Changed from string to Guid
        public string Transmission { get; set; } = "automatic";
        public string FuelType { get; set; } = "petrol";
        public int SeatingCapacity { get; set; } = 5;
        public bool HasAC { get; set; } = true;
        public string[]? PhotoUrls { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public int? IncludedKilometers { get; set; }
        public decimal? PricePerExtraKm { get; set; }
        public bool? MileageChargingEnabled { get; set; }
        // Optional per-vehicle daily rate (must be within category min/max)
        public decimal? DailyRate { get; set; }
        
        // Auto-population fields
        public string? TransmissionType { get; set; }
        public string[]? Features { get; set; }
        public Dictionary<string, object>? Specifications { get; set; }
        public Dictionary<string, object>? Inclusions { get; set; }
        public int? MileageAllowancePerDay { get; set; }
        public decimal? ExtraKmRate { get; set; }
    }

    public class UpdateVehicleRequest
    {
        public string? PlateNumber { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? CityId { get; set; }
        public string? Transmission { get; set; }
        public string? FuelType { get; set; }
        public int? SeatingCapacity { get; set; }
        public bool? HasAC { get; set; }
        public string[]? PhotoUrls { get; set; }
        public string? InsuranceDocumentUrl { get; set; }
        public string? RoadworthinessDocumentUrl { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public int? IncludedKilometers { get; set; }
        public decimal? PricePerExtraKm { get; set; }
        public bool? MileageChargingEnabled { get; set; }
        // Optional per-vehicle daily rate (must be within category min/max)
        public decimal? DailyRate { get; set; }
        
        // Auto-population fields
        public string? TransmissionType { get; set; }
        public string[]? Features { get; set; }
        public Dictionary<string, object>? Specifications { get; set; }
        public Dictionary<string, object>? Inclusions { get; set; }
        public int? MileageAllowancePerDay { get; set; }
        public decimal? ExtraKmRate { get; set; }
    }

    private static async Task<IResult> CreateVehicleAsync(
        ClaimsPrincipal principal,
        [FromBody] CreateVehicleRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.PlateNumber))
            return Results.BadRequest(new { error = "PlateNumber is required" });

        // Validate city if provided
        if (request.CityId.HasValue)
        {
            var cityExists = await db.Cities.AnyAsync(c => c.Id == request.CityId.Value && c.IsActive);
            if (!cityExists)
                return Results.BadRequest(new { error = "Invalid or inactive city selected" });
        }

        // Validate mileage settings if provided
        if (request.IncludedKilometers.HasValue || request.PricePerExtraKm.HasValue)
        {
            var mileageSettings = await GetMileageSettingsAsync(db);
            if (mileageSettings == null)
                return Results.BadRequest(new { error = "Mileage charging settings not configured by admin" });

            if (request.IncludedKilometers.HasValue && 
                request.IncludedKilometers.Value < mileageSettings.MinimumIncludedKilometers)
                return Results.BadRequest(new { error = $"Included kilometers must be at least {mileageSettings.MinimumIncludedKilometers} km" });

            if (request.PricePerExtraKm.HasValue &&
                (request.PricePerExtraKm.Value < mileageSettings.MinPricePerExtraKm ||
                 request.PricePerExtraKm.Value > mileageSettings.MaxPricePerExtraKm))
                return Results.BadRequest(new { error = $"Price per extra km must be between {mileageSettings.MinPricePerExtraKm} and {mileageSettings.MaxPricePerExtraKm}" });
        }

        // Validate daily rate if provided (must be within the category min/max)
        if (request.DailyRate.HasValue)
        {
            if (!request.CategoryId.HasValue)
                return Results.BadRequest(new { error = "Daily rate requires a category to be selected" });

            var catForRate = await db.CarCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value);
            if (catForRate is null)
                return Results.BadRequest(new { error = "Invalid category selected for daily rate" });

            if (request.DailyRate.Value < catForRate.MinDailyRate || request.DailyRate.Value > catForRate.MaxDailyRate)
                return Results.BadRequest(new { error = $"Daily rate must be between {catForRate.MinDailyRate:F2} and {catForRate.MaxDailyRate:F2}" });
        }

        var vehicle = new Vehicle
        {
            OwnerId = userId,
            PlateNumber = request.PlateNumber.Trim(),
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            CategoryId = request.CategoryId,
            CityId = request.CityId,
            Transmission = request.Transmission.Trim().ToLowerInvariant(),
            FuelType = request.FuelType.Trim().ToLowerInvariant(),
            SeatingCapacity = request.SeatingCapacity,
            HasAC = request.HasAC,
            Status = "pending_review",
            PhotosJson = request.PhotoUrls is null ? null : JsonSerializer.Serialize(request.PhotoUrls),
            IncludedKilometers = request.IncludedKilometers ?? 0,
            PricePerExtraKm = request.PricePerExtraKm ?? 0,
            MileageChargingEnabled = request.MileageChargingEnabled ?? true,
            DailyRate = request.DailyRate,
            
            // Auto-population fields
            TransmissionType = request.TransmissionType,
            FeaturesJson = request.Features is null ? null : JsonSerializer.Serialize(request.Features),
            SpecificationsJson = request.Specifications is null ? null : JsonSerializer.Serialize(request.Specifications),
            InclusionsJson = GetSyncedInclusionsJson(request.Inclusions, request.MileageAllowancePerDay, request.ExtraKmRate),
            MileageAllowancePerDay = request.MileageAllowancePerDay,
            ExtraKmRate = request.ExtraKmRate
        };
        // Set default availability window if not provided
        var now = DateTime.UtcNow.Date;
        vehicle.AvailableFrom = request.AvailableFrom ?? now;
        vehicle.AvailableUntil = request.AvailableUntil ?? now.AddYears(2);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/owner/vehicles/{vehicle.Id}", vehicle);
    }

    private static async Task<IResult> UpdateVehicleAsync(
        Guid vehicleId,
        ClaimsPrincipal principal,
        [FromBody] UpdateVehicleRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
            vehicle.PlateNumber = request.PlateNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.Make))
            vehicle.Make = request.Make.Trim();

        if (!string.IsNullOrWhiteSpace(request.Model))
            vehicle.Model = request.Model.Trim();

        if (request.Year.HasValue)
            vehicle.Year = request.Year.Value;

        if (request.CategoryId.HasValue)
            vehicle.CategoryId = request.CategoryId.Value;

        if (request.CityId.HasValue)
        {
            var cityExists = await db.Cities.AnyAsync(c => c.Id == request.CityId.Value && c.IsActive);
            if (!cityExists)
                return Results.BadRequest(new { error = "Invalid or inactive city selected" });
            
            vehicle.CityId = request.CityId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Transmission))
            vehicle.Transmission = request.Transmission.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(request.FuelType))
            vehicle.FuelType = request.FuelType.Trim().ToLowerInvariant();

        if (request.SeatingCapacity.HasValue)
            vehicle.SeatingCapacity = request.SeatingCapacity.Value;

        if (request.HasAC.HasValue)
            vehicle.HasAC = request.HasAC.Value;

        if (request.PhotoUrls is not null)
            vehicle.PhotosJson = JsonSerializer.Serialize(request.PhotoUrls);

        if (!string.IsNullOrWhiteSpace(request.InsuranceDocumentUrl))
            vehicle.InsuranceDocumentUrl = request.InsuranceDocumentUrl.Trim();

        if (!string.IsNullOrWhiteSpace(request.RoadworthinessDocumentUrl))
            vehicle.RoadworthinessDocumentUrl = request.RoadworthinessDocumentUrl.Trim();

        if (request.AvailableFrom.HasValue)
            vehicle.AvailableFrom = request.AvailableFrom.Value;

        if (request.AvailableUntil.HasValue)
            vehicle.AvailableUntil = request.AvailableUntil.Value;

        // Update mileage settings if provided
        if (request.IncludedKilometers.HasValue || request.PricePerExtraKm.HasValue || request.MileageChargingEnabled.HasValue)
        {
            var mileageSettings = await GetMileageSettingsAsync(db);
            if (mileageSettings == null)
                return Results.BadRequest(new { error = "Mileage charging settings not configured by admin" });

            if (request.IncludedKilometers.HasValue)
            {
                if (request.IncludedKilometers.Value < mileageSettings.MinimumIncludedKilometers)
                    return Results.BadRequest(new { error = $"Included kilometers must be at least {mileageSettings.MinimumIncludedKilometers} km" });
                vehicle.IncludedKilometers = request.IncludedKilometers.Value;
            }

            if (request.PricePerExtraKm.HasValue)
            {
                if (request.PricePerExtraKm.Value < mileageSettings.MinPricePerExtraKm ||
                    request.PricePerExtraKm.Value > mileageSettings.MaxPricePerExtraKm)
                    return Results.BadRequest(new { error = $"Price per extra km must be between {mileageSettings.MinPricePerExtraKm} and {mileageSettings.MaxPricePerExtraKm}" });
                vehicle.PricePerExtraKm = request.PricePerExtraKm.Value;
            }

            if (request.MileageChargingEnabled.HasValue)
                vehicle.MileageChargingEnabled = request.MileageChargingEnabled.Value;
        }

        // Validate and update daily rate if provided
        if (request.DailyRate.HasValue)
        {
            var effectiveCategoryId = request.CategoryId ?? vehicle.CategoryId;
            if (!effectiveCategoryId.HasValue)
                return Results.BadRequest(new { error = "Daily rate requires a category" });

            var catForRate = await db.CarCategories.FirstOrDefaultAsync(c => c.Id == effectiveCategoryId.Value);
            if (catForRate is null)
                return Results.BadRequest(new { error = "Invalid category for daily rate" });

            if (request.DailyRate.Value < catForRate.MinDailyRate || request.DailyRate.Value > catForRate.MaxDailyRate)
                return Results.BadRequest(new { error = $"Daily rate must be between {catForRate.MinDailyRate:F2} and {catForRate.MaxDailyRate:F2}" });

            vehicle.DailyRate = request.DailyRate.Value;
        }

        // Update auto-population fields if provided
        if (!string.IsNullOrWhiteSpace(request.TransmissionType))
            vehicle.TransmissionType = request.TransmissionType;

        if (request.Features is not null)
            vehicle.FeaturesJson = JsonSerializer.Serialize(request.Features);

        if (request.Specifications is not null)
            vehicle.SpecificationsJson = JsonSerializer.Serialize(request.Specifications);

        // Update mileage fields first
        if (request.MileageAllowancePerDay.HasValue)
            vehicle.MileageAllowancePerDay = request.MileageAllowancePerDay.Value;

        if (request.ExtraKmRate.HasValue)
            vehicle.ExtraKmRate = request.ExtraKmRate.Value;

        // Sync inclusions with current mileage values
        if (request.Inclusions is not null || request.MileageAllowancePerDay.HasValue || request.ExtraKmRate.HasValue)
        {
            var currentInclusions = request.Inclusions;
            // If inclusions not provided but mileage is, deserialize existing inclusions
            if (currentInclusions is null && !string.IsNullOrWhiteSpace(vehicle.InclusionsJson))
            {
                currentInclusions = JsonSerializer.Deserialize<Dictionary<string, object>>(vehicle.InclusionsJson);
            }
            
            vehicle.InclusionsJson = GetSyncedInclusionsJson(
                currentInclusions, 
                request.MileageAllowancePerDay ?? vehicle.MileageAllowancePerDay,
                request.ExtraKmRate ?? vehicle.ExtraKmRate
            );
        }

        await db.SaveChangesAsync();

        return Results.Ok(vehicle);
    }

    private static async Task<IResult> DeleteVehicleAsync(
        Guid vehicleId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        // Prevent deletion if there are active or pending bookings
        var hasActiveBookings = await db.Bookings.AnyAsync(b => b.VehicleId == vehicleId && b.Status != "cancelled" && b.Status != "completed");
        if (hasActiveBookings)
            return Results.BadRequest(new { error = "Cannot remove vehicle with active or pending bookings" });

        // Soft-delete by setting status to inactive
        vehicle.Status = "inactive";
        await db.SaveChangesAsync();

        return Results.Ok(new { vehicle.Id, vehicle.Status });
    }

    private static async Task<IResult> ForceDeleteVehicleAsync(
        Guid vehicleId,
        ClaimsPrincipal principal,
        AppDbContext db,
        IFileUploadService fileUploadService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        try
        {
            // Step 1: Get all bookings for this vehicle
            var bookings = await db.Bookings
                .Where(b => b.VehicleId == vehicleId)
                .ToListAsync();

            var bookingIds = bookings.Select(b => b.Id).ToList();
            int deletedInspections = 0;
            int deletedPayments = 0;
            int deletedFiles = 0;

            // Step 2: Clear inspection FK references in bookings (break circular dependency)
            foreach (var booking in bookings)
            {
                booking.PickupInspectionId = null;
                booking.ReturnInspectionId = null;
            }
            await db.SaveChangesAsync();
            
            // IMPORTANT: Clear change tracker to prevent EF from tracking old relationships
            db.ChangeTracker.Clear();

            // Step 3: Delete payment transactions linked to these bookings
            if (bookingIds.Any())
            {
                var payments = await db.PaymentTransactions
                    .Where(p => p.BookingId.HasValue && bookingIds.Contains(p.BookingId.Value))
                    .ToListAsync();
                
                if (payments.Any())
                {
                    db.PaymentTransactions.RemoveRange(payments);
                    deletedPayments = payments.Count;
                    await db.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                }
            }

            // Step 4: Now safe to delete inspections (bookings no longer reference them)
            if (bookingIds.Any())
            {
                var inspections = await db.Inspections
                    .Where(i => bookingIds.Contains(i.BookingId))
                    .ToListAsync();
                
                if (inspections.Any())
                {
                    db.Inspections.RemoveRange(inspections);
                    deletedInspections = inspections.Count;
                    await db.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                }
            }

            // Step 5: Delete bookings
            if (bookingIds.Any())
            {
                var bookingsToDelete = await db.Bookings
                    .Where(b => bookingIds.Contains(b.Id))
                    .ToListAsync();
                    
                if (bookingsToDelete.Any())
                {
                    db.Bookings.RemoveRange(bookingsToDelete);
                    await db.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                }
            }

            // Step 5: Delete vehicle photos
            if (!string.IsNullOrWhiteSpace(vehicle.PhotosJson))
            {
                try
                {
                    var photoUrls = JsonSerializer.Deserialize<List<string>>(vehicle.PhotosJson);
                    if (photoUrls != null)
                    {
                        foreach (var photoUrl in photoUrls)
                        {
                            if (await fileUploadService.DeleteFileAsync(photoUrl))
                                deletedFiles++;
                        }
                    }
                }
                catch { /* Ignore JSON parsing errors */ }
            }
            
            // Step 6: Delete insurance document
            if (!string.IsNullOrWhiteSpace(vehicle.InsuranceDocumentUrl))
            {
                if (await fileUploadService.DeleteFileAsync(vehicle.InsuranceDocumentUrl))
                    deletedFiles++;
            }
            
            // Step 7: Delete roadworthiness document
            if (!string.IsNullOrWhiteSpace(vehicle.RoadworthinessDocumentUrl))
            {
                if (await fileUploadService.DeleteFileAsync(vehicle.RoadworthinessDocumentUrl))
                    deletedFiles++;
            }

            // Step 8: Reload and delete the vehicle (was cleared from tracker)
            var vehicleToDelete = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
            if (vehicleToDelete != null)
            {
                db.Vehicles.Remove(vehicleToDelete);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { 
                success = true, 
                message = $"Vehicle '{vehicle.Make} {vehicle.Model}' ({vehicle.PlateNumber}) permanently deleted", 
                deletedInspections = deletedInspections,
                deletedPayments = deletedPayments,
                deletedBookings = bookingIds.Count,
                deletedFiles = deletedFiles
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to delete vehicle"
            );
        }
    }

    private static async Task<IResult> GetOwnerBookingsAsync(
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

        // Verify owner is active
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || (user.Role == "owner" && user.Status != "active"))
            return Results.Json(new { error = "Your owner account is pending verification. Please contact support." }, statusCode: 403);

        var query = db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.Renter).ThenInclude(u => u!.RenterProfile)
            .Include(b => b.Driver).ThenInclude(d => d!.DriverProfile)
            .Where(b => b.OwnerId == userId);

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
            success = true,
            total,
            page,
            pageSize,
            hasResults = total > 0,
            message = total == 0 ? "No bookings found. Bookings for your vehicles will appear here." : null,
            data = bookings.Select(b => new BookingResponse(b))
        });
    }

    private static async Task<IResult> GetOwnerDriversAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var drivers = await db.DriverProfiles
            .Include(d => d.User)
            .Where(d => d.OwnerEmployerId == userId && d.DriverType == "owner_employed")
            .Select(d => new
            {
                id = d.UserId,
                firstName = d.User != null ? d.User.FirstName : null,
                lastName = d.User != null ? d.User.LastName : null,
                phoneNumber = d.User != null ? d.User.Phone : null,
                email = d.User != null ? d.User.Email : null,
                licenseNumber = d.LicenseNumber,
                licenseExpiryDate = d.LicenseExpiryDate,
                profilePhotoUrl = d.PhotoUrl,
                averageRating = d.AverageRating,
                totalRides = d.TotalTrips,
                available = d.Available,
                verificationStatus = d.VerificationStatus,
                driverType = d.DriverType,
                createdAt = d.CreatedAt
            })
            .OrderByDescending(d => d.createdAt)
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            total = drivers.Count,
            message = drivers.Count == 0 ? "No drivers found. Drivers you employ will appear here." : null,
            data = drivers
        });
    }

    private static async Task<IResult> UploadVehicleImagesAsync(
        Guid vehicleId,
        ClaimsPrincipal principal,
        HttpRequest request,
        IFileUploadService fileUploadService,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Invalid form upload" });

        var uploadList = new List<(Stream stream, string fileName, string contentType)>();
        foreach (var f in request.Form.Files)
        {
            uploadList.Add((f.OpenReadStream(), f.FileName, f.ContentType));
        }

        var uploadedUrls = await fileUploadService.UploadMultipleFilesAsync(uploadList);
        // Convert to absolute URLs so the frontend on a different origin can load them
        var absoluteUrls = uploadedUrls.Select(u => request.AbsolutizeUrl(u)).ToList();

        // Support optional document-type tagging via query string ?docType=insurance|roadworthiness
        var docType = request.Query["docType"].ToString().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(docType))
        {
            // Apply first uploaded file as the document URL
            var first = absoluteUrls.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                if (docType == "insurance")
                    vehicle.InsuranceDocumentUrl = first;
                else if (docType == "roadworthiness" || docType == "ownership")
                    vehicle.RoadworthinessDocumentUrl = first;
                else
                {
                    var existing = string.IsNullOrWhiteSpace(vehicle.PhotosJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(vehicle.PhotosJson)!;
                    existing.AddRange(absoluteUrls);
                    vehicle.PhotosJson = JsonSerializer.Serialize(existing);
                }
            }
        }
        else
        {
            var existing = string.IsNullOrWhiteSpace(vehicle.PhotosJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(vehicle.PhotosJson)!;
            existing.AddRange(absoluteUrls);
            vehicle.PhotosJson = JsonSerializer.Serialize(existing);
        }
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, urls = absoluteUrls });
    }

    private static async Task<IResult> GetPayoutDetailsAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        return Results.Ok(new { payoutPreference = user.OwnerProfile?.PayoutPreference, payoutDetails = user.OwnerProfile?.PayoutDetailsJson == null ? null : JsonSerializer.Deserialize<object>(user.OwnerProfile.PayoutDetailsJson) });
    }

    private static async Task<IResult> GetVerificationStatusAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        if (user.OwnerProfile is null)
        {
            return Results.Ok(new
            {
                payoutVerificationStatus = "unverified",
                companyVerificationStatus = "unverified",
                hasPayoutDetails = false,
                hasPendingPayoutDetails = false,
                canReceivePayouts = false
            });
        }

        var hasPayoutDetails = !string.IsNullOrWhiteSpace(user.OwnerProfile.PayoutDetailsJson);
        var hasPendingPayoutDetails = !string.IsNullOrWhiteSpace(user.OwnerProfile.PayoutDetailsPendingJson);
        var payoutVerified = user.OwnerProfile.PayoutVerificationStatus == "verified";
        
        return Results.Ok(new
        {
            payoutVerificationStatus = user.OwnerProfile.PayoutVerificationStatus ?? "unverified",
            companyVerificationStatus = user.OwnerProfile.CompanyVerificationStatus ?? "unverified",
            hasPayoutDetails,
            hasPendingPayoutDetails,
            canReceivePayouts = hasPayoutDetails && payoutVerified
        });
    }

    private static async Task<IResult> UpdatePayoutDetailsAsync(
        ClaimsPrincipal principal,
        [FromBody] JsonElement payload,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        if (user.OwnerProfile is null)
        {
            user.OwnerProfile = new OwnerProfile { UserId = user.Id };
            db.OwnerProfiles.Add(user.OwnerProfile);
        }

        // Expect payload: { payoutPreference: 'bank'|'momo', payoutDetails: { ... } }
        if (payload.TryGetProperty("payoutPreference", out var pref))
            user.OwnerProfile.PayoutPreference = pref.GetString() ?? user.OwnerProfile.PayoutPreference;

        if (payload.TryGetProperty("payoutDetails", out var details))
        {
            // Store in pending and set status to pending for admin verification
            user.OwnerProfile.PayoutDetailsPendingJson = JsonSerializer.Serialize(details);
            user.OwnerProfile.PayoutVerificationStatus = "pending";
        }

        try
        {
            await db.SaveChangesAsync();
            return Results.Ok(new 
            { 
                success = true,
                message = "Payout details submitted for verification. Admin will review shortly.",
                verificationStatus = user.OwnerProfile.PayoutVerificationStatus
            });
        }
        catch (Exception ex)
        {
            return Results.Problem("Could not update owner profile: " + ex.Message);
        }
    }

    public record RequestPayoutRequest(decimal Amount, string Method, DateTime? PeriodStart, DateTime? PeriodEnd);

    private static async Task<IResult> RequestPayoutAsync(
        ClaimsPrincipal principal,
        [FromBody] RequestPayoutRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "Amount must be greater than zero" });

        // compute available balance
        var completedBookings = await db.Bookings.Where(b => b.OwnerId == userId && b.Status == "completed").ToListAsync();
        // Owner receives: rental amount + driver fees - platform commission
        var totalEarnings = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));
        var payouts = await db.Payouts.Where(p => p.OwnerId == userId).ToListAsync();
        var totalPaidOut = payouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
        var pendingPayout = payouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);
        var available = totalEarnings - totalPaidOut - pendingPayout;

        if (request.Amount > available)
            return Results.BadRequest(new { error = "Requested amount exceeds available balance" });

        var payout = new Payout
        {
            OwnerId = userId,
            Amount = request.Amount,
            Method = request.Method.ToLowerInvariant(),
            Status = "pending",
            PeriodStart = request.PeriodStart ?? DateTime.UtcNow.AddMonths(-1),
            PeriodEnd = request.PeriodEnd ?? DateTime.UtcNow
        };

        db.Payouts.Add(payout);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/owner/payouts/{payout.Id}", new { payout.Id, payout.Status, payout.Amount });
    }

    private static async Task<IResult> GetOwnerProfileAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.Phone,
            ownerProfile = user.OwnerProfile is null ? null : new
            {
                user.OwnerProfile.OwnerType,
                user.OwnerProfile.DisplayName,
                user.OwnerProfile.CompanyName,
                user.OwnerProfile.BusinessRegistrationNumber,
                user.OwnerProfile.PayoutPreference,
                user.OwnerProfile.BusinessPhone,
                user.OwnerProfile.BusinessAddress,
                user.OwnerProfile.GpsAddress,
                user.OwnerProfile.PickupInstructions,
                user.OwnerProfile.City,
                user.OwnerProfile.Region,
                payoutDetails = user.OwnerProfile.PayoutDetailsJson == null ? null : JsonSerializer.Deserialize<object>(user.OwnerProfile.PayoutDetailsJson),
                payoutVerificationStatus = user.OwnerProfile.PayoutVerificationStatus,
                payoutDetailsPending = user.OwnerProfile.PayoutDetailsPendingJson == null ? null : JsonSerializer.Deserialize<object>(user.OwnerProfile.PayoutDetailsPendingJson)
            }
        });
    }

    private static async Task<IResult> UpdateOwnerProfileAsync(
        ClaimsPrincipal principal,
        [FromBody] JsonElement payload,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        if (user.OwnerProfile is null)
        {
            user.OwnerProfile = new OwnerProfile { UserId = user.Id };
            db.OwnerProfiles.Add(user.OwnerProfile);
        }

        if (payload.TryGetProperty("displayName", out var dn))
            user.OwnerProfile.DisplayName = dn.GetString();

        if (payload.TryGetProperty("companyName", out var cn))
        {
            // Direct update - removed admin verification for simplicity
            user.OwnerProfile.CompanyName = cn.GetString();
            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit { UserId = user.Id, Field = "CompanyName", OldValue = user.OwnerProfile.CompanyName, NewValue = cn.GetString(), ChangedByUserId = user.Id });
        }

        if (payload.TryGetProperty("businessRegistrationNumber", out var brn))
        {
            // Direct update - removed admin verification for simplicity
            user.OwnerProfile.BusinessRegistrationNumber = brn.GetString();
            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit { UserId = user.Id, Field = "BusinessRegistrationNumber", OldValue = user.OwnerProfile.BusinessRegistrationNumber, NewValue = brn.GetString(), ChangedByUserId = user.Id });
        }

        if (payload.TryGetProperty("ownerType", out var ot))
            user.OwnerProfile.OwnerType = ot.GetString() ?? user.OwnerProfile.OwnerType;

        if (payload.TryGetProperty("payoutPreference", out var pp))
            user.OwnerProfile.PayoutPreference = pp.GetString() ?? user.OwnerProfile.PayoutPreference;

        if (payload.TryGetProperty("payoutDetails", out var pd))
        {
            // Store pending payout details for verification
            user.OwnerProfile.PayoutDetailsPendingJson = JsonSerializer.Serialize(pd);
            user.OwnerProfile.PayoutVerificationStatus = "pending";
            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit { UserId = user.Id, Field = "PayoutDetails", OldValue = user.OwnerProfile.PayoutDetailsJson, NewValue = user.OwnerProfile.PayoutDetailsPendingJson, ChangedByUserId = user.Id });
        }

        // Update contact and location fields
        if (payload.TryGetProperty("businessPhone", out var bp))
            user.OwnerProfile.BusinessPhone = bp.GetString();

        if (payload.TryGetProperty("businessAddress", out var ba))
            user.OwnerProfile.BusinessAddress = ba.GetString();

        if (payload.TryGetProperty("gpsAddress", out var ga))
            user.OwnerProfile.GpsAddress = ga.GetString();

        if (payload.TryGetProperty("pickupInstructions", out var pi))
            user.OwnerProfile.PickupInstructions = pi.GetString();

        if (payload.TryGetProperty("city", out var city))
            user.OwnerProfile.City = city.GetString();

        if (payload.TryGetProperty("region", out var region))
            user.OwnerProfile.Region = region.GetString();

        await db.SaveChangesAsync();

        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> GetOwnerEarningsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Calculate ALL completed bookings to get accurate total earnings
        // This matches the admin endpoint logic for consistency
        var allCompletedBookings = await db.Bookings
            .Where(b => b.OwnerId == userId && b.Status == "completed")
            .ToListAsync();

        // Owner receives: rental amount + driver fees - platform commission
        // This is the SAME calculation used in /admin/payouts/summary
        var totalEarnings = allCompletedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        // Get ALL payouts to calculate accurate available balance
        var allPayouts = await db.Payouts
            .Where(p => p.OwnerId == userId)
            .ToListAsync();

        var totalPaidOut = allPayouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
        var pendingPayout = allPayouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);

        // Include instant withdrawals in the calculation
        var allInstantWithdrawals = await db.InstantWithdrawals
            .Where(w => w.OwnerId == userId)
            .ToListAsync();

        var completedWithdrawals = allInstantWithdrawals.Where(w => w.Status == "completed").Sum(w => w.Amount);
        var pendingWithdrawals = allInstantWithdrawals.Where(w => w.Status == "pending" || w.Status == "processing").Sum(w => w.Amount);

        // Calculate available balance using the same logic as RequestInstantWithdrawalAsync
        var availableBalance = totalEarnings - totalPaidOut - pendingPayout - completedWithdrawals - pendingWithdrawals;

        // Apply date filtering only for display/breakdown purposes
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var periodBookings = allCompletedBookings
            .Where(b => b.ReturnDateTime >= start && b.ReturnDateTime <= end)
            .ToList();

        var periodPayouts = allPayouts
            .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
            .ToList();

        var periodWithdrawals = allInstantWithdrawals
            .Where(w => w.CreatedAt >= start && w.CreatedAt <= end)
            .ToList();

        var periodEarnings = periodBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        return Results.Ok(new
        {
            // Overall lifetime totals (matching admin calculation)
            totalEarnings,
            totalBookings = allCompletedBookings.Count,
            totalPaidOut,
            pendingPayout,
            completedWithdrawals,
            pendingWithdrawals,
            availableBalance,
            
            // Period-specific breakdown for display
            period = new { start, end },
            periodEarnings,
            periodBookings = periodBookings.Count,
            
            bookings = periodBookings.Select(b => new
            {
                b.Id,
                b.VehicleId,
                b.RentalAmount,
                b.DriverAmount,
                b.PlatformFee,
                ownerEarnings = b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0),
                b.ReturnDateTime
            }),
            payouts = periodPayouts.Select(p => new
            {
                p.Id,
                p.Amount,
                p.Status,
                p.CreatedAt,
                p.CompletedAt
            }),
            withdrawals = periodWithdrawals.Select(w => new
            {
                w.Id,
                w.Amount,
                w.FeeAmount,
                w.NetAmount,
                w.Status,
                w.CreatedAt,
                w.CompletedAt
            })
        });
    }

    private static async Task<IResult> GetOwnerPayoutsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Payouts
            .Where(p => p.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            hasResults = total > 0,
            message = total == 0 ? "No payout history found. Complete bookings to start earning." : null,
            data = payouts
        });
    }

    // Helper to get mileage settings
    private static async Task<MileageChargingSettings?> GetMileageSettingsAsync(AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (setting == null) return null;
        return JsonSerializer.Deserialize<MileageChargingSettings>(setting.ValueJson);
    }

    // Public endpoint for owners to get mileage settings constraints
    private static async Task<IResult> GetOwnerMileageSettingsAsync(AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (setting == null)
        {
            // Return default settings if not configured
            return Results.Ok(new 
            { 
                mileageChargingEnabled = false,
                minimumIncludedKilometers = 100,
                minPricePerExtraKm = 0.30m,
                maxPricePerExtraKm = 1.00m,
                defaultPricePerExtraKm = 0.50m
            });
        }

        var settings = JsonSerializer.Deserialize<MileageChargingSettings>(setting.ValueJson);
        if (settings == null)
        {
            return Results.Ok(new 
            { 
                mileageChargingEnabled = false,
                minimumIncludedKilometers = 100,
                minPricePerExtraKm = 0.30m,
                maxPricePerExtraKm = 1.00m,
                defaultPricePerExtraKm = 0.50m
            });
        }

        return Results.Ok(new
        {
            mileageChargingEnabled = settings.MileageChargingEnabled,
            minimumIncludedKilometers = settings.MinimumIncludedKilometers,
            minPricePerExtraKm = settings.MinPricePerExtraKm,
            maxPricePerExtraKm = settings.MaxPricePerExtraKm,
            defaultPricePerExtraKm = settings.DefaultPricePerExtraKm
        });
    }

    // Owner can view rental agreement acceptance for bookings on their vehicles
    private static async Task<IResult> GetOwnerBookingAgreementAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Verify this booking is for owner's vehicle
        if (booking.OwnerId != userId)
            return Results.Forbid();

        var acceptance = await db.RentalAgreementAcceptances
            .Include(a => a.Renter)
            .FirstOrDefaultAsync(a => a.BookingId == bookingId);

        if (acceptance is null)
            return Results.NotFound(new { error = "No rental agreement signed for this booking yet" });

        return Results.Ok(new
        {
            bookingId = acceptance.BookingId,
            bookingReference = booking.BookingReference,
            renterId = acceptance.RenterId,
            renterName = acceptance.Renter != null ? $"{acceptance.Renter.FirstName} {acceptance.Renter.LastName}" : null,
            renterEmail = acceptance.Renter?.Email,
            templateCode = acceptance.TemplateCode,
            templateVersion = acceptance.TemplateVersion,
            agreementSnapshot = acceptance.AgreementSnapshot,
            acceptedNoSmoking = acceptance.AcceptedNoSmoking,
            acceptedFinesAndTickets = acceptance.AcceptedFinesAndTickets,
            acceptedAccidentProcedure = acceptance.AcceptedAccidentProcedure,
            acceptedAt = acceptance.AcceptedAt,
            ipAddress = acceptance.IpAddress
        });
    }

    private static async Task<IResult> LookupVehicleDataAsync(
        int year,
        string make,
        string model,
        [FromQuery] string? trim,
        IVehicleDataService vehicleDataService,
        IAppConfigService configService)
    {
        if (year < 1900 || year > DateTime.UtcNow.Year + 2)
            return Results.BadRequest(new { error = "Invalid year" });

        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
            return Results.BadRequest(new { error = "Make and model are required" });

        // Fetch vehicle data from NHTSA or inference
        var lookupResult = await vehicleDataService.LookupVehicleDataAsync(year, make, model, trim);

        if (lookupResult == null)
            return Results.NotFound(new { error = "Unable to fetch vehicle data" });

        // Get global default mileage settings
        var defaultMileageAllowance = await configService.GetConfigValueAsync<int>("Vehicle:DefaultMileageAllowancePerDay", 600);
        var defaultExtraKmRate = await configService.GetConfigValueAsync<decimal>("Vehicle:DefaultExtraKmRate", 0.30m);

        // Build specifications JSON
        var specifications = new Dictionary<string, string>
        {
            { "Engine Size", lookupResult.EngineSize ?? "N/A" },
            { "Fuel Type", lookupResult.FuelType ?? "Petrol" },
            { "Fuel Efficiency", lookupResult.FuelEfficiency ?? "N/A" },
            { "Transmission", lookupResult.TransmissionType ?? "N/A" },
            { "Drivetrain", lookupResult.Drivetrain ?? "N/A" },
            { "Body Style", lookupResult.BodyStyle ?? "Sedan" },
            { "Seating Capacity", lookupResult.SeatingCapacity?.ToString() ?? "5" }
        };

        // Add any additional specs
        foreach (var spec in lookupResult.AdditionalSpecs)
        {
            if (!specifications.ContainsKey(spec.Key))
                specifications[spec.Key] = spec.Value;
        }

        // Build inclusions JSON
        var inclusions = new Dictionary<string, object>
        {
            { "mileageAllowancePerDay", defaultMileageAllowance },
            { "extraKmRate", defaultExtraKmRate },
            { "currency", "GHS" },
            { "protectionPlanRequired", true },
            { "roadside Assistance", "Depends on selected protection plan" },
            { "cancellationPolicy", "Free cancellation with 48hrs notice" }
        };

        return Results.Ok(new
        {
            year = lookupResult.Year,
            make = lookupResult.Make,
            model = lookupResult.Model,
            trim = lookupResult.Trim,
            fuelType = lookupResult.FuelType,
            transmissionType = lookupResult.TransmissionType,
            seatingCapacity = lookupResult.SeatingCapacity,
            features = lookupResult.Features,
            specifications = specifications,
            inclusions = inclusions,
            message = "Owner can override any of these values when creating/updating the vehicle"
        });
    }
}
