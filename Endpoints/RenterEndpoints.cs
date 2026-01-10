using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Extensions; // Absolutize URL helper
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GhanaHybridRentalApi.Endpoints;

public static class RenterEndpoints
{
    public static void MapRenterEndpoints(this IEndpointRouteBuilder app)
    {
        // Renter bookings
        app.MapGet("/api/v1/renter/bookings", GetRenterBookingsAsync)
            .RequireAuthorization("RenterOnly");

        // Document metadata endpoint
        async Task<IResult> GetDocumentAsync(Guid id, AppDbContext db, HttpContext httpContext)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id);
            if (doc is null) return Results.NotFound(new { error = "Document not found" });

            var url = doc.Url ?? string.Empty;
            url = httpContext.Request.AbsolutizeUrl(url);

            return Results.Ok(new
            {
                id = doc.Id,
                fileName = doc.FileName,
                url,
                contentType = doc.ContentType,
                size = doc.Size,
                uploadedAt = doc.UploadedAt
            });
        }

        // Public endpoints for website
        app.MapGet("/api/v1/cities", GetCitiesAsync);

        // Vehicle search and browse
        app.MapGet("/api/v1/categories", GetCategoriesAsync);

        app.MapGet("/api/v1/vehicles", SearchVehiclesAsync);

        app.MapGet("/api/v1/vehicles/{vehicleId:guid}", GetVehicleDetailsAsync);

        app.MapGet("/api/v1/vehicles/{vehicleId:guid}/availability", CheckVehicleAvailabilityAsync);

        // KYC and verification
        app.MapPost("/api/v1/renter/verification/documents", UploadVerificationDocumentAsync)
            .RequireAuthorization("RenterOnly");

        app.MapPut("/api/v1/renter/profile", UpdateRenterProfileAsync)
            .RequireAuthorization("RenterOnly");

        // Backwards compatible routes expected by frontend
        app.MapGet("/api/v1/renters/me", GetCurrentRenterProfileAsync)
            .RequireAuthorization("RenterOnly");

        app.MapGet("/api/v1/renters/me/documents", GetCurrentRenterDocumentsAsync)
            .RequireAuthorization("RenterOnly");

        // Public document lookup by id (returns metadata and absolute URL)
        app.MapGet("/api/v1/documents/{id:guid}", GetDocumentAsync);

        // Renter transactions and dashboard
        app.MapGet("/api/v1/renter/transactions", GetRenterTransactionsAsync)
            .RequireAuthorization("RenterOnly");

        app.MapGet("/api/v1/renter/dashboard", GetRenterDashboardAsync)
            .RequireAuthorization("RenterOnly");

        // Renter notifications
        app.MapGet("/api/v1/renter/notifications", GetRenterNotificationsAsync)
            .RequireAuthorization("RenterOnly");

        app.MapPut("/api/v1/renter/notifications/{id:guid}/read", MarkNotificationReadAsync)
            .RequireAuthorization("RenterOnly");

        // Renter rental agreements - view agreements they have signed
        app.MapGet("/api/v1/renter/rental-agreements", GetRenterAgreementsAsync)
            .RequireAuthorization("RenterOnly");
        app.MapGet("/api/v1/renter/bookings/{bookingId:guid}/rental-agreement", GetRenterBookingAgreementAsync)
            .RequireAuthorization("RenterOnly");
        app.MapGet("/api/v1/renter/bookings/{bookingId:guid}/rental-agreement/download", DownloadRenterAgreementAsync)
            .RequireAuthorization("RenterOnly");
    }

    private static async Task<IResult> GetCurrentRenterProfileAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.Include(u => u.RenterProfile).FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        var profile = user.RenterProfile;

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            phone = user.Phone,
            firstName = user.FirstName,
            lastName = user.LastName,
            renterProfile = profile is null ? null : new
            {
                profile.FullName,
                profile.Nationality,
                profile.Dob,
                profile.VerificationStatus
            }
        });
    }

    private static async Task<IResult> GetCurrentRenterDocumentsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        HttpContext httpContext)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.RenterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        if (profile is null)
            return Results.NotFound(new { error = "Renter profile not found" });

        var documents = string.IsNullOrWhiteSpace(profile.DocumentsJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(profile.DocumentsJson) ?? new Dictionary<string, string>();

        // Resolve any document IDs to absolute URLs for easier frontend consumption
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var idMap = documents
            .Where(kv => Guid.TryParse(kv.Value, out _))
            .Select(kv => Guid.Parse(kv.Value))
            .ToList();

        Dictionary<Guid, string> resolved = new();
        if (idMap.Any())
        {
            var docs = await db.Documents.Where(d => idMap.Contains(d.Id)).ToListAsync();
            foreach (var d in docs)
            {
                var url = d.Url ?? string.Empty;
                url = httpContext.Request.AbsolutizeUrl(url);
                resolved[d.Id] = url;
            }

            // Replace IDs with resolved URLs in returned map
            var keys = documents.Keys.ToList();
            foreach (var k in keys)
            {
                if (Guid.TryParse(documents[k], out var gid) && resolved.TryGetValue(gid, out var absUrl))
                {
                    documents[k] = absUrl;
                }
            }
        }

        return Results.Ok(new { success = true, data = documents });
    }

    private static async Task<IResult> GetRenterBookingsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Bookings
            .Include(b => b.Vehicle)
            .Where(b => b.RenterId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status.ToLowerInvariant());

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
            message = total == 0 ? "No bookings found. Your booking history will appear here once you make a reservation." : null,
            data = bookings
        });
    }

    private static async Task<IResult> GetCategoriesAsync(AppDbContext db)
    {
        var categories = await db.CarCategories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.DefaultDailyRate,
                c.MinDailyRate,
                c.MaxDailyRate,
                c.DefaultDepositAmount,
                c.RequiresDriver
            })
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            total = categories.Count,
            hasResults = categories.Any(),
            message = categories.Any() ? null : "No vehicle categories available at this time.",
            data = categories
        });
    }

    private static async Task<IResult> SearchVehiclesAsync(
        AppDbContext db,
        HttpContext httpContext,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? cityId,
        [FromQuery] string? transmission,
        [FromQuery] int? minSeats,
        [FromQuery] bool? hasAC,
        [FromQuery] decimal? maxDailyRate,
        [FromQuery] DateTime? availableFrom,
        [FromQuery] DateTime? availableUntil,
        [FromQuery] bool? withDriver,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Get platform fee for pricing display
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }
        
        var query = db.Vehicles
            .Include(v => v.Category)
            .Include(v => v.Owner)
            .Include(v => v.City)
            .Where(v => v.Status == "active" && v.DeletedAt == null);

        if (categoryId.HasValue)
            query = query.Where(v => v.CategoryId == categoryId.Value);

        if (cityId.HasValue)
            query = query.Where(v => v.CityId == cityId.Value);

        if (!string.IsNullOrWhiteSpace(transmission))
            query = query.Where(v => v.Transmission == transmission.ToLowerInvariant());

        if (minSeats.HasValue)
            query = query.Where(v => v.SeatingCapacity >= minSeats.Value);

        if (hasAC.HasValue)
            query = query.Where(v => v.HasAC == hasAC.Value);

        if (maxDailyRate.HasValue)
            query = query.Where(v => (v.DailyRate ?? v.Category!.DefaultDailyRate) <= maxDailyRate.Value);

        if (withDriver.HasValue)
            query = query.Where(v => v.Category!.RequiresDriver == withDriver.Value);

        // Check availability if dates provided
        List<Guid> conflictingVehicleIds = new();
        if (availableFrom.HasValue && availableUntil.HasValue)
        {
            // Convert to UTC if not already
            var fromUtc = availableFrom.Value.Kind == DateTimeKind.Utc 
                ? availableFrom.Value 
                : DateTime.SpecifyKind(availableFrom.Value, DateTimeKind.Utc);
            var untilUtc = availableUntil.Value.Kind == DateTimeKind.Utc 
                ? availableUntil.Value 
                : DateTime.SpecifyKind(availableUntil.Value, DateTimeKind.Utc);

            conflictingVehicleIds = db.Bookings
                .Where(b => b.Status != "cancelled" &&
                           b.Status != "completed" &&
                           b.PickupDateTime < untilUtc &&
                           b.ReturnDateTime > fromUtc)
                .Select(b => b.VehicleId)
                .ToList();

            query = query.Where(v => !conflictingVehicleIds.Contains(v.Id));
        }

        var total = await query.CountAsync();
        var vehicles = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        string? Absolutize(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return null;
            if (Uri.IsWellFormedUriString(u, UriKind.Absolute)) return u!;
            if (u.StartsWith("/")) return baseUrl + u;
            return baseUrl + "/" + u;
        }

        List<string> SafeParsePhotos(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); } catch { return new List<string>(); }
        }

        var response = new
        {
            success = true,
            total,
            page,
            pageSize,
            hasResults = total > 0,
            message = total == 0 ? "No vehicles found matching your search criteria. Try adjusting your filters or search dates." : null,
            searchDates = availableFrom.HasValue && availableUntil.HasValue ? new
            {
                from = availableFrom.Value.Kind == DateTimeKind.Utc 
                    ? availableFrom.Value 
                    : DateTime.SpecifyKind(availableFrom.Value, DateTimeKind.Utc),
                until = availableUntil.Value.Kind == DateTimeKind.Utc 
                    ? availableUntil.Value 
                    : DateTime.SpecifyKind(availableUntil.Value, DateTimeKind.Utc)
            } : null,

            data = vehicles.Select(v => new
            {
                cityId = v.CityId,
                city = v.City is null ? null : new { v.City.Id, v.City.Name },
                v.Id,
                v.PlateNumber,
                v.Make,
                v.Model,
                v.Year,
                v.Transmission,
                v.FuelType,
                v.SeatingCapacity,
                v.HasAC,
                v.Status,
                isAvailable = true,
                
                // New auto-populated fields
                transmissionType = v.TransmissionType,
                features = ParseJsonArray(v.FeaturesJson),
                specifications = ParseJsonObject(v.SpecificationsJson),
                inclusions = ParseJsonObject(v.InclusionsJson),
                mileageAllowancePerDay = v.MileageAllowancePerDay,
                extraKmRate = v.ExtraKmRate,
                
                category = v.Category is null ? null : new
                {
                    v.Category.Id,
                    v.Category.Name,
                    v.Category.Description,
                    v.Category.DefaultDailyRate,
                    v.Category.DefaultDepositAmount,
                    v.Category.RequiresDriver
                },
                    mileageTerms = new
                {
                    enabled = v.MileageChargingEnabled,
                    includedKilometers = v.IncludedKilometers,
                    pricePerExtraKm = v.PricePerExtraKm,
                    currency = "GHS"
                },
                    photos = SafeParsePhotos(v.PhotosJson).Select(p => Absolutize(p)).ToList(),
                // Document extraction: prefer explicit fields, otherwise scan photo filenames for doc keywords
                    insuranceDocumentUrl = Absolutize(
                        !string.IsNullOrWhiteSpace(v.InsuranceDocumentUrl)
                            ? v.InsuranceDocumentUrl
                            : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("insurance") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")))
                    ),
                    roadworthinessDocumentUrl = Absolutize(
                        !string.IsNullOrWhiteSpace(v.RoadworthinessDocumentUrl)
                            ? v.RoadworthinessDocumentUrl
                            : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("roadworth") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")))
                    ),
                    // Resolved daily rate (vehicle override or category default)
                    dailyRate = (v.DailyRate ?? v.Category!.DefaultDailyRate),
                    vehicleDailyRate = v.DailyRate,
                    
                    // Pricing transparency for guests
                    pricing = new
                    {
                        dailyRate = (v.DailyRate ?? v.Category!.DefaultDailyRate),
                        platformFeePercentage,
                        depositAmount = v.Category!.DefaultDepositAmount,
                        currency = "GHS",
                        // Example 1-day pricing
                        estimatedDailyTotal = (v.DailyRate ?? v.Category!.DefaultDailyRate) * (1 + (platformFeePercentage / 100m))
                    }
            }).ToList()
        };

        return Results.Ok(response);
    }

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
        catch { return new List<string>(); }
    }

    private static Dictionary<string, object> ParseJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
        try 
        { 
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(); 
        }
        catch { return new Dictionary<string, object>(); }
    }

    private static async Task<IResult> GetVehicleDetailsAsync(
        Guid vehicleId,
        AppDbContext db,
        HttpContext httpContext)
    {
        // Get platform fee for pricing display
        decimal platformFeePercentage = 15.0m;
        var platformFeeSetting = await db.GlobalSettings
            .FirstOrDefaultAsync(s => s.Key == "PlatformFeePercentage");
        if (platformFeeSetting != null)
        {
            try { platformFeePercentage = JsonSerializer.Deserialize<decimal>(platformFeeSetting.ValueJson); } catch { }
        }
        
        var vehicle = await db.Vehicles
            .Include(v => v.Category)
            .Include(v => v.Owner)
                .ThenInclude(o => o!.OwnerProfile)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        // Get recent bookings count for credibility
        var completedBookingsCount = await db.Bookings
            .Where(b => b.VehicleId == vehicleId && b.Status == "completed")
            .CountAsync();
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        string? Absolutize(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return null;
            if (Uri.IsWellFormedUriString(u, UriKind.Absolute)) return u!;
            if (u.StartsWith("/")) return baseUrl + u;
            return baseUrl + "/" + u;
        }

        return Results.Ok(new
        {
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.Transmission,
            vehicle.FuelType,
            vehicle.SeatingCapacity,
            vehicle.HasAC,
            vehicle.Status,
            vehicle.CityId,
            
            // New auto-populated fields
            transmissionType = vehicle.TransmissionType,
            features = ParseJsonArray(vehicle.FeaturesJson),
            specifications = ParseJsonObject(vehicle.SpecificationsJson),
            inclusions = ParseJsonObject(vehicle.InclusionsJson),
            mileageAllowancePerDay = vehicle.MileageAllowancePerDay,
            extraKmRate = vehicle.ExtraKmRate,
            
            city = vehicle.City is null ? null : new { vehicle.City.Id, vehicle.City.Name },
            category = vehicle.Category is null ? null : new
            {
                vehicle.Category.Id,
                vehicle.Category.Name,
                vehicle.Category.Description,
                vehicle.Category.DefaultDailyRate,
                vehicle.Category.DefaultDepositAmount,
                vehicle.Category.RequiresDriver
            },
            // Resolved per-vehicle pricing
            dailyRate = vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate,
            vehicleDailyRate = vehicle.DailyRate,
            mileageTerms = new
            {
                enabled = vehicle.MileageChargingEnabled,
                includedKilometers = vehicle.IncludedKilometers,
                pricePerExtraKm = vehicle.PricePerExtraKm,
                currency = "GHS"
            },
            owner = new
            {
                displayName = vehicle.Owner?.OwnerProfile?.DisplayName,
                ownerType = vehicle.Owner?.OwnerProfile?.OwnerType
            },
            completedBookingsCount,
            photos = string.IsNullOrWhiteSpace(vehicle.PhotosJson)
                ? new List<string?>()
                : SafeParsePhotos(vehicle.PhotosJson).Select(p => Absolutize(p)).ToList(),
            insuranceDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(vehicle.InsuranceDocumentUrl)
                ? vehicle.InsuranceDocumentUrl
                : SafeParsePhotos(vehicle.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("insurance") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")))),
            roadworthinessDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(vehicle.RoadworthinessDocumentUrl)
                ? vehicle.RoadworthinessDocumentUrl
                : SafeParsePhotos(vehicle.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("roadworth") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")) ) ),
            
            // Transparent pricing for guests
            pricing = new
            {
                dailyRate = vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate,
                platformFeePercentage,
                platformFeeAmount = (vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate ?? 0) * (platformFeePercentage / 100m),
                depositAmount = vehicle.Category?.DefaultDepositAmount,
                currency = "GHS",
                // Sample 1-day pricing breakdown
                sampleDayBreakdown = new
                {
                    rentalAmount = vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate,
                    platformFee = (vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate ?? 0) * (platformFeePercentage / 100m),
                    totalPerDay = (vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate ?? 0) * (1 + (platformFeePercentage / 100m)),
                    deposit = vehicle.Category?.DefaultDepositAmount,
                    grandTotal = ((vehicle.DailyRate ?? vehicle.Category?.DefaultDailyRate ?? 0) * (1 + (platformFeePercentage / 100m))) + (vehicle.Category?.DefaultDepositAmount ?? 0)
                },
                note = "Use /api/v1/bookings/calculate-price for exact multi-day pricing"
            }
        });
    }

    private static List<string?> SafeParsePhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string?>();
        try { return JsonSerializer.Deserialize<List<string?>>(json) ?? new List<string?>(); } catch { return new List<string?>(); }
    }

    private static async Task<IResult> GetRenterTransactionsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.PaymentTransactions.Where(t => t.UserId == userId).AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.CreatedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(t => t.Type == type.ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = items.Select(t => new
            {
                t.Id,
                t.BookingId,
                t.Type,
                t.Status,
                t.Amount,
                t.Method,
                t.Reference,
                t.ExternalTransactionId,
                t.CreatedAt,
                t.CompletedAt
            })
        });
    }

    private static async Task<IResult> GetRenterDashboardAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var now = DateTime.UtcNow;

        var upcomingBookings = await db.Bookings
            .Where(b => b.RenterId == userId && b.PickupDateTime >= now)
            .OrderBy(b => b.PickupDateTime)
            .Take(5)
            .Select(b => new { b.Id, b.BookingReference, b.PickupDateTime, b.ReturnDateTime, b.Status, b.TotalAmount, b.VehicleId })
            .ToListAsync();

        var activeCount = await db.Bookings.CountAsync(b => b.RenterId == userId && (b.Status == "pending_payment" || b.Status == "confirmed" || b.Status == "ongoing"));
        var upcomingCount = await db.Bookings.CountAsync(b => b.RenterId == userId && b.PickupDateTime >= now);
        var totalSpent = await db.PaymentTransactions.Where(t => t.UserId == userId && t.Type == "payment" && t.Status == "completed").SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Results.Ok(new
        {
            totalBookings = await db.Bookings.CountAsync(b => b.RenterId == userId),
            activeBookings = activeCount,
            upcomingBookings = upcomingCount,
            totalSpent,
            next = upcomingBookings
        });
    }

    private static async Task<IResult> GetRenterNotificationsAsync(
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

    private static async Task<IResult> MarkNotificationReadAsync(
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

    private static async Task<IResult> CheckVehicleAvailabilityAsync(
        Guid vehicleId,
        AppDbContext db,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.DeletedAt == null);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (vehicle.Status != "active")
            return Results.Ok(new 
            { 
                available = false, 
                reason = "Vehicle not active",
                vehicleId,
                requestedDates = new
                {
                    from = startDate,
                    until = endDate
                }
            });

        // Check for conflicting bookings
        var conflictingBooking = await db.Bookings
            .Where(b => b.VehicleId == vehicleId &&
                       b.Status != "cancelled" &&
                       b.Status != "completed" &&
                       b.PickupDateTime < endDate &&
                       b.ReturnDateTime > startDate)
            .OrderBy(b => b.PickupDateTime)
            .FirstOrDefaultAsync();

        if (conflictingBooking != null)
        {
            return Results.Ok(new
            {
                available = false,
                reason = "Vehicle is already booked for the requested dates",
                vehicleId,
                requestedDates = new
                {
                    from = startDate,
                    until = endDate
                },
                conflictingBooking = new
                {
                    conflictingBooking.BookingReference,
                    pickupDateTime = conflictingBooking.PickupDateTime,
                    returnDateTime = conflictingBooking.ReturnDateTime
                },
                nextAvailableFrom = conflictingBooking.ReturnDateTime
            });
        }

        return Results.Ok(new 
        { 
            available = true,
            vehicleId,
            requestedDates = new
            {
                from = startDate,
                until = endDate
            }
        });
    }

    private static async Task<IResult> UploadVerificationDocumentAsync(
        ClaimsPrincipal principal,
        [FromBody] UploadDocumentRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.RenterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        if (profile is null)
            return Results.NotFound(new { error = "Renter profile not found" });

        var documents = string.IsNullOrWhiteSpace(profile.DocumentsJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(profile.DocumentsJson) ?? new Dictionary<string, string>();

        // If caller provided a documentId prefer it and store the document id; otherwise store the URL (but try to resolve to a document row)
        if (request.DocumentId.HasValue)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId.Value);
            if (doc is null)
                return Results.BadRequest(new { error = "Invalid DocumentId" });

            documents[request.DocumentType] = doc.Id.ToString();
        }
        else
        {
            documents[request.DocumentType] = request.DocumentUrl;

            // Try to resolve an existing Document by URL and store the ID instead for consistency
            if (!string.IsNullOrWhiteSpace(request.DocumentUrl))
            {
                var existing = await db.Documents.FirstOrDefaultAsync(d => d.Url == request.DocumentUrl);
                if (existing != null)
                    documents[request.DocumentType] = existing.Id.ToString();
            }
        }

        profile.DocumentsJson = JsonSerializer.Serialize(documents);

        // Update verification status if ID and license uploaded
        if (documents.ContainsKey("nationalId") && documents.ContainsKey("driverLicense"))
        {
            if (profile.VerificationStatus == "unverified")
                profile.VerificationStatus = "basic_verified";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            userId = profile.UserId,
            verificationStatus = profile.VerificationStatus,
            documentsUploaded = documents // return mapping docType => document id or url
        });
    }

    private static async Task<IResult> UpdateRenterProfileAsync(
        ClaimsPrincipal principal,
        [FromBody] UpdateRenterProfileRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.RenterProfiles.FirstOrDefaultAsync(r => r.UserId == userId);
        if (profile is null)
            return Results.NotFound(new { error = "Renter profile not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        // Handle email update with validation and uniqueness check
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            // Basic email format validation
            if (!request.Email.Contains("@") || !request.Email.Contains("."))
                return Results.BadRequest(new { error = "Invalid email format" });

            // Check if email is already in use by another user
            var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
            if (emailExists)
                return Results.BadRequest(new { error = "Email already in use by another account" });

            // Log the change for audit
            db.ProfileChangeAudits.Add(new ProfileChangeAudit
            {
                UserId = userId,
                Field = "Email",
                OldValue = user.Email,
                NewValue = request.Email,
                ChangedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            user.Email = request.Email;
        }

        // Handle phone update with validation and uniqueness check
        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != user.Phone)
        {
            // Normalize phone number
            var normalizedPhone = request.Phone.Trim().Replace(" ", "").Replace("-", "");
            if (!normalizedPhone.StartsWith("+"))
            {
                // Add Ghana country code if missing
                if (normalizedPhone.StartsWith("0"))
                    normalizedPhone = "+233" + normalizedPhone.Substring(1);
                else if (!normalizedPhone.StartsWith("233"))
                    normalizedPhone = "+233" + normalizedPhone;
                else
                    normalizedPhone = "+" + normalizedPhone;
            }

            // Check if phone is already in use by another user
            var phoneExists = await db.Users.AnyAsync(u => u.Phone == normalizedPhone && u.Id != userId);
            if (phoneExists)
                return Results.BadRequest(new { error = "Phone number already in use by another account" });

            // Log the change for audit
            db.ProfileChangeAudits.Add(new ProfileChangeAudit
            {
                UserId = userId,
                Field = "Phone",
                OldValue = user.Phone,
                NewValue = normalizedPhone,
                ChangedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            user.Phone = normalizedPhone;
            user.PhoneVerified = false; // Reset verification status when phone changes
        }

        // Update first name and last name on User table
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.FullName))
            profile.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.Nationality))
            profile.Nationality = request.Nationality;

        if (request.Dob.HasValue)
            profile.Dob = request.Dob.Value;

        // Driver's License (for self-drive)
        if (!string.IsNullOrWhiteSpace(request.DriverLicenseNumber))
            profile.DriverLicenseNumber = request.DriverLicenseNumber;

        if (request.DriverLicenseExpiryDate.HasValue)
            profile.DriverLicenseExpiryDate = request.DriverLicenseExpiryDate.Value;

        if (!string.IsNullOrWhiteSpace(request.DriverLicensePhotoUrl))
            profile.DriverLicensePhotoUrl = request.DriverLicensePhotoUrl;

        // National ID / Ghana Card (for bookings with driver)
        if (!string.IsNullOrWhiteSpace(request.NationalIdNumber))
            profile.NationalIdNumber = request.NationalIdNumber;

        if (!string.IsNullOrWhiteSpace(request.NationalIdPhotoUrl))
            profile.NationalIdPhotoUrl = request.NationalIdPhotoUrl;

        // Passport (alternative to Ghana Card for bookings with driver)
        if (!string.IsNullOrWhiteSpace(request.PassportNumber))
            profile.PassportNumber = request.PassportNumber;

        if (request.PassportExpiryDate.HasValue)
            profile.PassportExpiryDate = request.PassportExpiryDate.Value;

        if (!string.IsNullOrWhiteSpace(request.PassportPhotoUrl))
            profile.PassportPhotoUrl = request.PassportPhotoUrl;

        // Address Information
        if (!string.IsNullOrWhiteSpace(request.StreetAddress))
            profile.StreetAddress = request.StreetAddress;

        if (!string.IsNullOrWhiteSpace(request.City))
            profile.City = request.City;

        // Emergency Contact
        if (!string.IsNullOrWhiteSpace(request.EmergencyContactName))
            profile.EmergencyContactName = request.EmergencyContactName;

        if (!string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
            profile.EmergencyContactPhone = request.EmergencyContactPhone;

        // Update verification status
        var hasDriverLicense = !string.IsNullOrWhiteSpace(profile.DriverLicenseNumber) && 
                              !string.IsNullOrWhiteSpace(profile.DriverLicensePhotoUrl) &&
                              profile.DriverLicenseExpiryDate.HasValue;

        var hasNationalId = !string.IsNullOrWhiteSpace(profile.NationalIdNumber) && 
                           !string.IsNullOrWhiteSpace(profile.NationalIdPhotoUrl);

        var hasPassport = !string.IsNullOrWhiteSpace(profile.PassportNumber) && 
                         !string.IsNullOrWhiteSpace(profile.PassportPhotoUrl) &&
                         profile.PassportExpiryDate.HasValue;

        if (hasDriverLicense && (hasNationalId || hasPassport))
            profile.VerificationStatus = "driver_verified";
        else if (hasNationalId || hasPassport)
            profile.VerificationStatus = "basic_verified";

        await db.SaveChangesAsync();

        return Results.Ok(profile);
    }

    // Public endpoint to get active cities
    private static async Task<IResult> GetCitiesAsync(AppDbContext db)
    {
        var cities = await db.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                c.CountryCode,
                c.IsActive
            })
            .ToListAsync();

        return Results.Ok(cities);
    }

    // Renter can view all their signed rental agreements
    private static async Task<IResult> GetRenterAgreementsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.RentalAgreementAcceptances
            .Include(a => a.Booking)
            .Where(a => a.RenterId == userId);

        var total = await query.CountAsync();

        var agreements = await query
            .OrderByDescending(a => a.AcceptedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                bookingId = a.BookingId,
                bookingReference = a.Booking != null ? a.Booking.BookingReference : null,
                templateCode = a.TemplateCode,
                templateVersion = a.TemplateVersion,
                acceptedAt = a.AcceptedAt,
                acceptedNoSmoking = a.AcceptedNoSmoking,
                acceptedFinesAndTickets = a.AcceptedFinesAndTickets,
                acceptedAccidentProcedure = a.AcceptedAccidentProcedure
            })
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = agreements
        });
    }

    // Renter can view specific rental agreement they signed
    private static async Task<IResult> GetRenterBookingAgreementAsync(
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

        // Verify this is renter's booking
        if (booking.RenterId != userId)
            return Results.Forbid();

        var acceptance = await db.RentalAgreementAcceptances
            .FirstOrDefaultAsync(a => a.BookingId == bookingId && a.RenterId == userId);

        if (acceptance is null)
            return Results.NotFound(new { error = "No rental agreement signed for this booking" });

        return Results.Ok(new
        {
            bookingId = acceptance.BookingId,
            bookingReference = booking.BookingReference,
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

    // Renter can download their signed rental agreement as PDF
    private static async Task<IResult> DownloadRenterAgreementAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
            
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Verify this is renter's booking
        if (booking.RenterId != userId)
            return Results.Forbid();

        var acceptance = await db.RentalAgreementAcceptances
            .FirstOrDefaultAsync(a => a.BookingId == bookingId && a.RenterId == userId);

        if (acceptance is null)
            return Results.NotFound(new { error = "No rental agreement signed for this booking" });

        // Generate real PDF for rental agreement
        var pdfBytes = GenerateAgreementPdf(booking, acceptance);

        return Results.File(
            pdfBytes,
            "application/pdf",
            $"rental-agreement-{booking.BookingReference}.pdf"
        );
    }

    private static byte[] GenerateAgreementPdf(Models.Booking booking, Models.RentalAgreementAcceptance acceptance)
    {
        // Use fully qualified alias to avoid conflict with Models.Document
        var pdfDocument = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("VEHICLE RENTAL AGREEMENT")
                        .Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    
                    column.Item().AlignCenter().Text("RYVE RENTAL")
                        .Bold().FontSize(14).FontColor(Colors.Blue.Darken1);
                    
                    column.Item().PaddingTop(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    // Agreement metadata
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Agreement Date: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm} UTC").FontSize(9);
                        row.RelativeItem().AlignRight().Text($"Booking Ref: {booking.BookingReference}").FontSize(9);
                    });
                    column.Item().Text($"Template: {acceptance.TemplateCode} v{acceptance.TemplateVersion}").FontSize(9);

                    // Renter Information
                    column.Item().PaddingTop(15).Text("RENTER INFORMATION").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Column(renterColumn =>
                    {
                        renterColumn.Item().Text($"Name: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
                        renterColumn.Item().Text($"Email: {booking.Renter?.Email}");
                        renterColumn.Item().Text($"Phone: {booking.Renter?.Phone}");
                    });

                    // Vehicle Information
                    column.Item().PaddingTop(15).Text("VEHICLE INFORMATION").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Column(vehicleColumn =>
                    {
                        vehicleColumn.Item().Text($"Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}");
                        vehicleColumn.Item().Text($"Plate Number: {booking.Vehicle?.PlateNumber}");
                    });

                    // Rental Period
                    column.Item().PaddingTop(15).Text("RENTAL PERIOD").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Column(periodColumn =>
                    {
                        periodColumn.Item().Text($"Pickup: {booking.PickupDateTime:yyyy-MM-dd HH:mm}");
                        periodColumn.Item().Text($"Return: {booking.ReturnDateTime:yyyy-MM-dd HH:mm}");
                    });

                    // Agreement Terms
                    column.Item().PaddingTop(15).Text("AGREEMENT TERMS").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Text(acceptance.AgreementSnapshot ?? "[Agreement content]").FontSize(10);

                    // Acceptance Confirmations
                    column.Item().PaddingTop(15).Text("ACCEPTANCE CONFIRMATIONS").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Column(acceptColumn =>
                    {
                        acceptColumn.Item().Text($"✓ No Smoking Policy: {(acceptance.AcceptedNoSmoking ? "ACCEPTED" : "NOT ACCEPTED")}");
                        acceptColumn.Item().Text($"✓ Fines & Tickets Responsibility: {(acceptance.AcceptedFinesAndTickets ? "ACCEPTED" : "NOT ACCEPTED")}");
                        acceptColumn.Item().Text($"✓ Accident Procedure: {(acceptance.AcceptedAccidentProcedure ? "ACCEPTED" : "NOT ACCEPTED")}");
                    });

                    // Digital Signature
                    column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10)
                        .Text("DIGITAL SIGNATURE").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                    column.Item().PaddingTop(5).PaddingLeft(10).Column(signColumn =>
                    {
                        signColumn.Item().Text($"Signed By: {booking.Renter?.FirstName} {booking.Renter?.LastName}").Bold();
                        signColumn.Item().Text($"Date & Time: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm:ss} UTC");
                        signColumn.Item().Text($"IP Address: {acceptance.IpAddress}");
                    });
                });

                page.Footer().AlignCenter().Column(column =>
                {
                    column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5)
                        .Text("This is a legally binding digital agreement.").FontSize(8).Italic();
                    column.Item().Text("Acceptance was recorded electronically with audit trail.").FontSize(8).Italic();
                });
            });
        });

        return pdfDocument.GeneratePdf();
    }

    private static string GenerateAgreementDocument(Models.Booking booking, Models.RentalAgreementAcceptance acceptance)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    VEHICLE RENTAL AGREEMENT");
        sb.AppendLine("                         RYVE RENTAL");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Agreement Date: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Booking Reference: {booking.BookingReference}");
        sb.AppendLine($"Template Version: {acceptance.TemplateCode} v{acceptance.TemplateVersion}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("RENTER INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Name: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
        sb.AppendLine($"Email: {booking.Renter?.Email}");
        sb.AppendLine($"Phone: {booking.Renter?.Phone}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("VEHICLE INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}");
        sb.AppendLine($"Plate Number: {booking.Vehicle?.PlateNumber}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("RENTAL PERIOD");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Pickup: {booking.PickupDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Return: {booking.ReturnDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("AGREEMENT TERMS");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine(acceptance.AgreementSnapshot ?? "[Agreement content]");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("ACCEPTANCE CONFIRMATIONS");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"✓ No Smoking Policy: {(acceptance.AcceptedNoSmoking ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine($"✓ Fines & Tickets Responsibility: {(acceptance.AcceptedFinesAndTickets ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine($"✓ Accident Procedure: {(acceptance.AcceptedAccidentProcedure ? "ACCEPTED" : "NOT ACCEPTED")}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("DIGITAL SIGNATURE");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Signed By: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
        sb.AppendLine($"Date & Time: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"IP Address: {acceptance.IpAddress}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("This is a legally binding digital agreement.");
        sb.AppendLine("Acceptance was recorded electronically with audit trail.");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }
}

public record UploadDocumentRequest(string DocumentType, string DocumentUrl, Guid? DocumentId);

public record UpdateRenterProfileRequest(
    string? Email,
    string? Phone,
    string? FirstName,
    string? LastName,
    string? FullName, 
    string? Nationality, 
    DateTime? Dob,
    string? DriverLicenseNumber,
    DateTime? DriverLicenseExpiryDate,
    string? DriverLicensePhotoUrl,
    string? NationalIdNumber,
    string? NationalIdPhotoUrl,
    string? PassportNumber,
    DateTime? PassportExpiryDate,
    string? PassportPhotoUrl,
    string? StreetAddress,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);
