using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Services;
using GhanaHybridRentalApi.Extensions; // Absolutize URL helper

namespace GhanaHybridRentalApi.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // User management
        app.MapGet("/api/v1/admin/users", GetUsersAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/renters", GetRentersAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/users/{userId:guid}/status", UpdateUserStatusAsync)
            .RequireAuthorization("AdminOnly");
        
        // Renter management endpoints
        app.MapPost("/api/v1/admin/renters/{renterId:guid}/verify-license", VerifyRenterLicenseAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/renters/{renterId:guid}/reject-license", RejectRenterLicenseAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/renters/{renterId:guid}/suspend", SuspendRenterAsync)
            .RequireAuthorization("AdminOnly");

        // Car categories
        app.MapGet("/api/v1/admin/categories", GetCategoriesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/categories", UpsertCategoryAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/api/v1/admin/categories/{categoryId:guid}", DeleteCategoryAsync)
            .RequireAuthorization("AdminOnly");

        // Cities
        app.MapGet("/api/v1/admin/cities", GetCitiesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/cities", CreateCityAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/cities/{cityId:guid}/deactivate", DeactivateCityAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/api/v1/admin/cities/{cityId:guid}", DeleteCityAsync)
            .RequireAuthorization("AdminOnly");

        // Global settings
        app.MapGet("/api/v1/admin/settings", GetGlobalSettingsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/settings/{key}", UpsertGlobalSettingAsync)
            .RequireAuthorization("AdminOnly");

        // Mileage charging settings
        app.MapGet("/api/v1/admin/settings/mileage", GetMileageSettingsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/settings/mileage", UpdateMileageSettingsAsync)
            .RequireAuthorization("AdminOnly");

        // Notification settings
        app.MapGet("/api/v1/admin/settings/notifications", GetNotificationSettingsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/settings/notifications", UpdateNotificationSettingsAsync)
            .RequireAuthorization("AdminOnly");

        // Notification jobs (create / list)
        app.MapPost("/api/v1/admin/notifications", CreateNotificationJobAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/notifications/jobs", ListNotificationJobsAsync)
            .RequireAuthorization("AdminOnly");

        // App config
        app.MapGet("/api/v1/admin/app-config", GetAppConfigAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/app-config/group", GetAppConfigGroupAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/app-config", UpsertAppConfigAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/app-config/security/jwt", UpdateJwtSettingsAsync)
            .RequireAuthorization("AdminOnly");
        
        // App domain configuration (for email links, etc.)
        app.MapGet("/api/v1/admin/app-domain", GetAppDomainAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/app-domain", UpdateAppDomainAsync)
            .RequireAuthorization("AdminOnly");

        // Vehicle moderation
        app.MapGet("/api/v1/admin/vehicles", GetAllVehiclesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/vehicles/pending", GetPendingVehiclesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/vehicles/{vehicleId:guid}/status", UpdateVehicleStatusAsync)
            .RequireAuthorization("AdminOnly");

        // Payout management
        app.MapGet("/api/v1/admin/payouts", GetPayoutsAsync)
            .RequireAuthorization("AdminOnly");

        // Documents admin endpoints are defined further below (aggregate vehicle/profile docs)

        app.MapPost("/api/v1/admin/payouts", CreatePayoutAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/payouts/{payoutId:guid}/execute", ExecutePayoutAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/payouts/{payoutId:guid}/status", UpdatePayoutStatusAsync)
            .RequireAuthorization("AdminOnly");

        // Analytics & Metrics
        app.MapGet("/api/v1/admin/metrics/overview", GetMetricsOverviewAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/metrics/bookings", GetBookingMetricsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/metrics/revenue", GetRevenueMetricsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/metrics/owners/top", GetTopOwnersAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/metrics/vehicles/top", GetTopVehiclesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/metrics/owner-payouts", GetOwnerPayoutsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/analytics/cities", GetCityAnalyticsAsync)
            .RequireAuthorization("AdminOnly");

        // Owner verification queue (also available via /api/v1/admin/account-requests for frontend compatibility)
        app.MapGet("/api/v1/admin/owner-verifications", GetOwnerVerificationsAsync)
            .RequireAuthorization("AdminOnly");
        // Alias route expected by frontend
        app.MapGet("/api/v1/admin/account-requests", GetOwnerVerificationsAsync)
            .RequireAuthorization("AdminOnly");
        
        // Admin documents overview (aggregates vehicle/profile documents)
        app.MapGet("/api/v1/admin/documents", GetDocumentsAsync)
            .RequireAuthorization("AdminOnly");

        // Document verification endpoints
        app.MapPut("/api/v1/admin/documents/renter/{userId:guid}/verify", VerifyRenterDocumentsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/documents/driver/{userId:guid}/verify", VerifyDriverDocumentsAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/owner-verifications/{userId:guid}/approve", ApproveOwnerVerificationAsync)
            .RequireAuthorization("AdminOnly");
        // Alias approve/reject for account-requests
        app.MapPost("/api/v1/admin/account-requests/{userId:guid}/approve", ApproveOwnerVerificationAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owner-verifications/{userId:guid}/reject", RejectOwnerVerificationAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/account-requests/{userId:guid}/reject", RejectOwnerVerificationAsync)
            .RequireAuthorization("AdminOnly");
        
        // Owner account approval/rejection (for new pending accounts)
        app.MapPost("/api/v1/admin/owners/{userId:guid}/approve", ApproveOwnerAccountAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owners/{userId:guid}/reject", RejectOwnerAccountAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owners/{userId:guid}/activate", ActivateOwnerAccountAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owners/{userId:guid}/deactivate", DeactivateOwnerAccountAsync)
            .RequireAuthorization("AdminOnly");

        // Owner payout method verification
        app.MapGet("/api/v1/admin/owners/{userId:guid}/payout-details", GetOwnerPayoutDetailsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owners/{userId:guid}/payout-details/verify", VerifyOwnerPayoutMethodAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/owners/{userId:guid}/payout-details/reject", RejectOwnerPayoutMethodAsync)
            .RequireAuthorization("AdminOnly");

        // Partner application management (listing handled in IntegrationPartnerEndpoints.cs)
        app.MapPost("/api/v1/admin/partner-applications/{partnerId:guid}/approve", ApprovePartnerApplicationAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/api/v1/admin/partner-applications/{partnerId:guid}/reject", RejectPartnerApplicationAsync)
            .RequireAuthorization("AdminOnly");

        
        // Owner management (CRUD)
        app.MapGet("/api/v1/admin/owners", GetOwnersAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/owners/{userId:guid}", GetOwnerDetailsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/owners/{userId:guid}", UpdateOwnerAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/api/v1/admin/owners/{userId:guid}", DeleteOwnerAsync)
            .RequireAuthorization("AdminOnly");
        
        // Vehicle management
        app.MapDelete("/api/v1/admin/vehicles/{vehicleId:guid}", DeleteVehicleAsync)
            .RequireAuthorization("AdminOnly");
        
        app.MapDelete("/api/v1/admin/vehicles/{vehicleId:guid}/force", ForceDeleteVehicleAsync)
            .RequireAuthorization("AdminOnly");
        
        // Admin request information from owner about a vehicle
        app.MapPost("/api/v1/admin/vehicles/{vehicleId:guid}/request-info", RequestVehicleInfoAsync)
            .RequireAuthorization("AdminOnly");

        // Admin send a custom notification to vehicle owner (subject + message)
        app.MapPost("/api/v1/admin/vehicles/{vehicleId:guid}/notify-owner", NotifyOwnerAsync)
            .RequireAuthorization("AdminOnly");
        
        // Test email configuration
        app.MapPost("/api/v1/admin/test-email", SendTestEmailAsync)
            .RequireAuthorization("AdminOnly");

        // Debug endpoint: send via Azure Communication Services directly and return operation id
        app.MapPost("/api/v1/admin/send-test-email-azure", SendTestEmailAzureAsync)
            .RequireAuthorization("AdminOnly");

        // Maintenance endpoint to add missing DB columns for notifications
        // Require Admin role and header 'X-Confirm-Action: add-notification-columns'
        app.MapPost("/api/v1/admin/maintenance/add-notification-columns", AdminAddNotificationColumnsAsync)
            .RequireAuthorization("AdminOnly");

        // Admin mileage charge override (manually add mileage charge to booking)
        app.MapPost("/api/v1/admin/bookings/{bookingId:guid}/charges/mileage", AdminMileageEndpoints.AddMileageChargeOverrideAsync)
            .RequireAuthorization("AdminOnly");

        // Convenience: get specific Email:Smtp group
        app.MapGet("/api/v1/admin/app-config/email-smtp", async (IAppConfigService cfg) => await cfg.GetConfigGroupAsync("Email:Smtp")).RequireAuthorization("AdminOnly");

        // Admin personal notifications endpoints
        app.MapGet("/api/v1/admin/my-notifications", GetAdminNotificationsAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/my-notifications/{id:guid}/read", MarkAdminNotificationReadAsync)
            .RequireAuthorization("AdminOnly");
        app.MapDelete("/api/v1/admin/my-notifications/{id:guid}", DeleteAdminNotificationAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/my-notifications/mark-all-read", MarkAllAdminNotificationsReadAsync)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/api/v1/admin/notification-preferences", GetAdminNotificationPreferencesAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPut("/api/v1/admin/notification-preferences", UpdateAdminNotificationPreferencesAsync)
            .RequireAuthorization("AdminOnly");
    }



    private static async Task<IResult> GetUsersAsync(
        AppDbContext db,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role.ToLowerInvariant());

            // Only filter by status if explicitly provided
            // This means by default, ALL users are shown (including suspended/deactivated)
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(u => u.Status == status.ToLowerInvariant());

            var total = await query.CountAsync();
            
            // Get users without profiles first to test
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Phone,
                    u.Role,
                    u.Status,
                    u.PhoneVerified,
                    u.FirstName,
                    u.LastName,
                    u.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                data = users
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }

    private static async Task<IResult> GetRentersAsync(
        AppDbContext db,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = db.RenterProfiles
                .AsNoTracking()
                .Include(r => r.User)
                .AsQueryable();

            // Search by name, email, or phone
            if (!string.IsNullOrWhiteSpace(search) && search != "undefined")
            {
                var searchLower = search.ToLowerInvariant();
                query = query.Where(r => 
                    (r.User != null && (
                        r.User.FirstName.ToLower().Contains(searchLower) ||
                        r.User.LastName.ToLower().Contains(searchLower) ||
                        r.User.Email.ToLower().Contains(searchLower) ||
                        (r.User.Phone != null && r.User.Phone.Contains(searchLower))
                    ))
                );
            }

            // Filter by verification status if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.VerificationStatus == status.ToLowerInvariant());
            }

            var total = await query.CountAsync();
            
            var renters = await query
                .OrderByDescending(r => r.User != null ? r.User.CreatedAt : DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    userId = r.UserId,
                    renterId = r.UserId,
                    email = r.User != null ? r.User.Email : null,
                    phone = r.User != null ? r.User.Phone : null,
                    firstName = r.User != null ? r.User.FirstName : null,
                    lastName = r.User != null ? r.User.LastName : null,
                    fullName = r.FullName,
                    nationality = r.Nationality,
                    dob = r.Dob,
                    userStatus = r.User != null ? r.User.Status : null,
                    verificationStatus = r.VerificationStatus,
                    // Driver's License
                    driverLicenseNumber = r.DriverLicenseNumber,
                    driverLicenseExpiryDate = r.DriverLicenseExpiryDate,
                    driverLicensePhotoUrl = r.DriverLicensePhotoUrl,
                    // National ID
                    nationalIdNumber = r.NationalIdNumber,
                    nationalIdPhotoUrl = r.NationalIdPhotoUrl,
                    // Passport
                    passportNumber = r.PassportNumber,
                    passportExpiryDate = r.PassportExpiryDate,
                    passportPhotoUrl = r.PassportPhotoUrl,
                    createdAt = r.User != null ? r.User.CreatedAt : DateTime.MinValue
                })
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                data = renters
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }

    private static async Task<IResult> UpdateUserStatusAsync(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        var requestedStatus = request.Status.ToLowerInvariant();
        
        // Accept "verified" as alias for "active" (more intuitive for UI)
        if (requestedStatus == "verified")
            requestedStatus = "active";

        var validStatuses = new[] { "active", "pending", "suspended" };
        if (!validStatuses.Contains(requestedStatus))
            return Results.BadRequest(new { error = "Invalid status. Valid values: active, pending, suspended, verified (alias for active)" });

        user.Status = requestedStatus;
        await db.SaveChangesAsync();

        return Results.Ok(new { user.Id, user.Status });
    }

    private static async Task<IResult> GetCategoriesAsync(AppDbContext db, [FromQuery] bool includeInactive = false)
    {
        var query = db.CarCategories.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var categories = await query.ToListAsync();
        return Results.Ok(categories);
    }

    private static async Task<IResult> DeleteCategoryAsync(Guid categoryId, AppDbContext db, [FromQuery] bool hard = false)
    {
        var category = await db.CarCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category == null)
            return Results.NotFound(new { error = "Category not found" });

        if (hard)
        {
            // Prevent hard delete if vehicles exist
            var hasVehicles = await db.Vehicles.AnyAsync(v => v.CategoryId == categoryId);
            if (hasVehicles)
                return Results.BadRequest(new { error = "Category has vehicles assigned. Reassign or set hard=false to soft delete." });

            db.CarCategories.Remove(category);
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Category hard-deleted" });
        }
        else
        {
            category.IsActive = false;
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Category soft-deleted (deactivated)" });
        }
    }

    private static async Task<IResult> UpsertCategoryAsync(
        [FromBody] CarCategory category,
        AppDbContext db)
    {
        if (category.Id == Guid.Empty)
        {
            category.Id = Guid.NewGuid();
            db.CarCategories.Add(category);
        }
        else
        {
            var existing = await db.CarCategories.FirstOrDefaultAsync(c => c.Id == category.Id);
            if (existing is null)
            {
                db.CarCategories.Add(category);
            }
            else
            {
                existing.Name = category.Name;
                existing.Description = category.Description;
                existing.DefaultDailyRate = category.DefaultDailyRate;
                existing.MinDailyRate = category.MinDailyRate;
                existing.MaxDailyRate = category.MaxDailyRate;
                existing.DefaultDepositAmount = category.DefaultDepositAmount;
                existing.RequiresDriver = category.RequiresDriver;
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(category);
    }

    private static async Task<IResult> GetGlobalSettingsAsync(AppDbContext db)
    {
        var settings = await db.GlobalSettings.ToListAsync();
        var result = settings.Select(s => new
        {
            s.Key,
            Value = JsonSerializer.Deserialize<object>(s.ValueJson)
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> UpsertGlobalSettingAsync(
        string key,
        [FromBody] JsonElement value,
        AppDbContext db)
    {
        var json = JsonSerializer.Serialize(value);
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            setting = new GlobalSetting
            {
                Key = key,
                ValueJson = json
            };
            db.GlobalSettings.Add(setting);
        }
        
        await db.SaveChangesAsync();
        return Results.Ok(new { key, value });
    }

    private static async Task<IResult> GetAllVehiclesAsync(
        AppDbContext db, 
        HttpContext httpContext,
        string? status = null,
        Guid? ownerId = null,
        Guid? cityId = null)
    {
        var query = db.Vehicles
            .Include(v => v.Owner)
            .Include(v => v.Category)
            .Include(v => v.City)
            .AsQueryable();

        // Apply filters if provided
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status.ToLowerInvariant());

        if (ownerId.HasValue)
            query = query.Where(v => v.OwnerId == ownerId.Value);

        if (cityId.HasValue)
            query = query.Where(v => v.CityId == cityId.Value);

        var vehicles = await query
            .OrderByDescending(v => v.Id)
            .ToListAsync();

        // Use helper to ensure correct scheme (handles proxies and upgrades http->https when needed)
        string? Absolutize(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return null;
            return httpContext.Request.AbsolutizeUrl(u);
        }

        List<string> SafeParsePhotos(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        var result = vehicles.Select(v => new
        {
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
            
            // Auto-populated fields
            transmissionType = v.TransmissionType,
            features = ParseJsonArray(v.FeaturesJson),
            specifications = ParseJsonObject(v.SpecificationsJson),
            inclusions = ParseJsonObject(v.InclusionsJson),
            mileageAllowancePerDay = v.MileageAllowancePerDay,
            extraKmRate = v.ExtraKmRate,
            
            // Mileage terms
            mileageTerms = new
            {
                enabled = v.MileageChargingEnabled,
                includedKilometers = v.IncludedKilometers,
                pricePerExtraKm = v.PricePerExtraKm,
                currency = "GHS"
            },
            
            cityId = v.CityId,
            city = v.City is null ? null : new { v.City.Id, v.City.Name },
            category = v.Category is null ? null : new { v.Category.Id, v.Category.Name, v.Category.DefaultDailyRate },
            dailyRate = v.DailyRate ?? v.Category?.DefaultDailyRate,
            vehicleDailyRate = v.DailyRate,
            owner = v.Owner is null ? null : new 
            { 
                v.Owner.Id, 
                v.Owner.Email, 
                v.Owner.Phone,
                v.Owner.FirstName,
                v.Owner.LastName,
                name = !string.IsNullOrWhiteSpace(v.Owner.FirstName) || !string.IsNullOrWhiteSpace(v.Owner.LastName)
                    ? $"{v.Owner.FirstName} {v.Owner.LastName}".Trim()
                    : v.Owner.Email
            },
            photos = SafeParsePhotos(v.PhotosJson).Select(p => Absolutize(p)).ToList(),
            insuranceDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(v.InsuranceDocumentUrl) ? v.InsuranceDocumentUrl : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("insurance") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")))),
            roadworthinessDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(v.RoadworthinessDocumentUrl) ? v.RoadworthinessDocumentUrl : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("roadworth") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct"))))
        });

        return Results.Ok(new 
        { 
            total = result.Count(), 
            filters = new { status, ownerId, cityId },
            data = result 
        });
    }

    private static async Task<IResult> GetPendingVehiclesAsync(AppDbContext db, HttpContext httpContext)
    {
        var vehicles = await db.Vehicles
            .Include(v => v.Owner)
            .Include(v => v.Category)
            .Include(v => v.City)
            .Where(v => v.Status == "pending_review")
            .OrderBy(v => v.Id)
            .ToListAsync();

        // Use helper to ensure correct scheme (handles proxies and upgrades http->https when needed)
        string? Absolutize(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return null;
            return httpContext.Request.AbsolutizeUrl(u);
        }

        List<string> SafeParsePhotos(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        var result = vehicles.Select(v => new
        {
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
            
            // New auto-populated fields for admin review
            transmissionType = v.TransmissionType,
            features = ParseJsonArray(v.FeaturesJson),
            specifications = ParseJsonObject(v.SpecificationsJson),
            inclusions = ParseJsonObject(v.InclusionsJson),
            mileageAllowancePerDay = v.MileageAllowancePerDay,
            extraKmRate = v.ExtraKmRate,
            
            cityId = v.CityId,
            city = v.City is null ? null : new { v.City.Id, v.City.Name },
            category = v.Category is null ? null : new { v.Category.Id, v.Category.Name, v.Category.DefaultDailyRate },
            dailyRate = v.DailyRate ?? v.Category?.DefaultDailyRate,
            vehicleDailyRate = v.DailyRate,
            owner = v.Owner is null ? null : new { v.Owner.Id, v.Owner.Email, v.Owner.Phone },
            photos = SafeParsePhotos(v.PhotosJson).Select(p => Absolutize(p)).ToList(),
            insuranceDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(v.InsuranceDocumentUrl) ? v.InsuranceDocumentUrl : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("insurance") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")))),
            roadworthinessDocumentUrl = Absolutize(!string.IsNullOrWhiteSpace(v.RoadworthinessDocumentUrl) ? v.RoadworthinessDocumentUrl : SafeParsePhotos(v.PhotosJson).FirstOrDefault(p => p != null && (p.ToLowerInvariant().Contains("roadworth") || p.ToLowerInvariant().Contains("ownership") || p.ToLowerInvariant().Contains("mot") || p.ToLowerInvariant().Contains("nct")) ))
        });

        return Results.Ok(new { total = result.Count(), data = result });
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

    private static async Task<IResult> UpdateVehicleStatusAsync(
        Guid vehicleId,
        [FromBody] UpdateVehicleStatusRequest request,
        AppDbContext db)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        var validStatuses = new[] { "active", "inactive", "suspended", "rejected" };
        if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid status" });

        var newStatus = request.Status.ToLowerInvariant();

        // If activating a vehicle, ensure required documents exist (insurance + roadworthiness)
        if (newStatus == "active")
        {
            var hasInsurance = !string.IsNullOrWhiteSpace(vehicle.InsuranceDocumentUrl)
                               || (!string.IsNullOrWhiteSpace(vehicle.PhotosJson) && vehicle.PhotosJson.ToLowerInvariant().Contains("insurance"));
            var hasRoadworth = !string.IsNullOrWhiteSpace(vehicle.RoadworthinessDocumentUrl)
                               || (!string.IsNullOrWhiteSpace(vehicle.PhotosJson) && (vehicle.PhotosJson.ToLowerInvariant().Contains("roadworth") || vehicle.PhotosJson.ToLowerInvariant().Contains("ownership")));

            if (!hasInsurance || !hasRoadworth)
            {
                return Results.BadRequest(new { error = "Cannot activate vehicle: missing required documents (insurance and roadworthiness). Owners should upload these documents before activation." });
            }
        }

        vehicle.Status = newStatus;
        await db.SaveChangesAsync();

        return Results.Ok(new { vehicle.Id, vehicle.Status });
    }

    private static async Task<IResult> RequestVehicleInfoAsync(
        Guid vehicleId,
        [FromBody] RequestVehicleInfoRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        INotificationService notificationService)
    {
        var adminIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.Include(v => v.Owner).FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (vehicle.Owner == null)
            return Results.BadRequest(new { error = "Vehicle has no owner" });

        // Record audit
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit
        {
            UserId = vehicle.OwnerId,
            Field = "VehicleRequestInfo",
            OldValue = null,
            NewValue = request.Notes,
            ChangedByUserId = adminId
        });

        await db.SaveChangesAsync();

        var owner = vehicle.Owner;
        var subject = "Request for Vehicle Information";
        var message = $"An admin has requested more information about your vehicle {vehicle.Make} {vehicle.Model} ({vehicle.PlateNumber}). Notes: {request.Notes}";

        var sent = await notificationService.SendOwnerNotificationAsync(owner, subject, message);

        if (!sent)
            return Results.Accepted($"/api/v1/admin/vehicles/{vehicleId}", new { success = true, message = "Request recorded; owner has no contact or notification failed" });

        return Results.Ok(new { success = true, message = "Request recorded and owner notified" });
    }

    private class NotifyOwnerRequest
    {
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }

    private static async Task<IResult> NotifyOwnerAsync(
        Guid vehicleId,
        [FromBody] NotifyOwnerRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        INotificationService notificationService)
    {
        var adminIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
            return Results.Unauthorized();

        var vehicle = await db.Vehicles.Include(v => v.Owner).FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (vehicle.Owner == null)
            return Results.BadRequest(new { error = "Vehicle has no owner" });

        // Record audit
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit
        {
            UserId = vehicle.OwnerId,
            Field = "VehicleNotifyOwner",
            OldValue = null,
            NewValue = request.Message,
            ChangedByUserId = adminId
        });

        await db.SaveChangesAsync();

        var subject = string.IsNullOrWhiteSpace(request.Subject) ? "Message from admin" : request.Subject;
        var sent = await notificationService.SendOwnerNotificationAsync(vehicle.Owner, subject, request.Message ?? string.Empty);

        if (!sent)
            return Results.Accepted($"/api/v1/admin/vehicles/{vehicleId}", new { success = true, message = "Notification recorded; owner has no contact or notification failed" });

        return Results.Ok(new { success = true, message = "Notification recorded and owner notified" });
    }

    private static async Task<IResult> GetPayoutsAsync(
        AppDbContext db,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Payouts
            .Include(p => p.Owner)
            .AsQueryable();

        if (ownerId.HasValue)
            query = query.Where(p => p.OwnerId == ownerId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var payouts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get count of scheduled payouts that are due (for admin visibility)
        var scheduledDueCount = 0;
        var scheduledDueAmount = 0m;
        
        // Get all owners with verified payout details
        var verifiedOwners = await db.Users
            .Include(u => u.OwnerProfile)
            .Where(u => u.Role == "owner" && u.OwnerProfile != null && u.OwnerProfile.PayoutVerificationStatus == "verified")
            .ToListAsync();

        foreach (var owner in verifiedOwners)
        {
            var profile = owner.OwnerProfile!;

            // Get last payout date
            var lastPayout = await db.Payouts
                .Where(p => p.OwnerId == owner.Id && p.Status == "completed")
                .OrderByDescending(p => p.CompletedAt)
                .FirstOrDefaultAsync();

            var lastPayoutDate = lastPayout?.CompletedAt ?? owner.CreatedAt;
            var nextPayoutDate = CalculateNextPayoutDate(lastPayoutDate, profile.PayoutFrequency);

            // Calculate available balance
            var completedBookings = await db.Bookings
                .Where(b => b.OwnerId == owner.Id && b.Status == "completed")
                .ToListAsync();

            var totalEarnings = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));
            var ownerPayouts = await db.Payouts.Where(p => p.OwnerId == owner.Id).ToListAsync();
            var totalPaidOut = ownerPayouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
            var pendingPayout = ownerPayouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);

            var instantWithdrawals = await db.InstantWithdrawals.Where(w => w.OwnerId == owner.Id).ToListAsync();
            var completedWithdrawals = instantWithdrawals.Where(w => w.Status == "completed").Sum(w => w.Amount);
            var pendingWithdrawals = instantWithdrawals.Where(w => w.Status == "pending" || w.Status == "processing").Sum(w => w.Amount);

            var available = totalEarnings - totalPaidOut - pendingPayout - completedWithdrawals - pendingWithdrawals;

            // Check if payout is due today
            if (nextPayoutDate.Date <= DateTime.UtcNow.Date && available >= profile.MinimumPayoutAmount)
            {
                scheduledDueCount++;
                scheduledDueAmount += available;
            }
        }

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            scheduledDue = new
            {
                count = scheduledDueCount,
                totalAmount = scheduledDueAmount,
                message = scheduledDueCount > 0 
                    ? $"{scheduledDueCount} scheduled payout(s) are due. Check /api/v1/admin/payouts/scheduled/due for details."
                    : "No scheduled payouts are due."
            },
            data = payouts.Select(p => new PayoutResponse(
                p.Id,
                p.OwnerId,
                p.Amount,
                p.Currency,
                p.Status,
                p.Method,
                p.Reference,
                p.PeriodStart,
                p.PeriodEnd,
                p.CreatedAt,
                p.CompletedAt,
                string.IsNullOrWhiteSpace(p.BookingIdsJson) ? 0 : JsonSerializer.Deserialize<List<Guid>>(p.BookingIdsJson)!.Count
            ))
        });
    }

    // Helper method to calculate next payout date based on frequency
    private static DateTime CalculateNextPayoutDate(DateTime lastPayoutDate, string frequency)
    {
        return frequency.ToLowerInvariant() switch
        {
            "daily" => lastPayoutDate.AddDays(1),
            "weekly" => lastPayoutDate.AddDays(7),
            "biweekly" => lastPayoutDate.AddDays(14),
            "monthly" => lastPayoutDate.AddMonths(1),
            _ => lastPayoutDate.AddDays(7) // Default to weekly
        };
    }

    private static async Task<IResult> CreatePayoutAsync(
        [FromBody] CreatePayoutRequest request,
        AppDbContext db)
    {
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == request.OwnerId && u.Role == "owner");
        if (owner is null)
            return Results.BadRequest(new { error = "Owner not found" });

        if (request.Amount <= 0)
            return Results.BadRequest(new { error = "Amount must be greater than zero" });

        var payout = new Payout
        {
            OwnerId = request.OwnerId,
            Amount = request.Amount,
            Method = request.Method.ToLowerInvariant(),
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Status = "pending",
            Reference = $"PAYOUT-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
            BookingIdsJson = JsonSerializer.Serialize(request.BookingIds)
        };

        db.Payouts.Add(payout);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/payouts/{payout.Id}", new PayoutResponse(
            payout.Id,
            payout.OwnerId,
            payout.Amount,
            payout.Currency,
            payout.Status,
            payout.Method,
            payout.Reference,
            payout.PeriodStart,
            payout.PeriodEnd,
            payout.CreatedAt,
            payout.CompletedAt,
            request.BookingIds.Count
        ));
    }

    private static async Task<IResult> UpdatePayoutStatusAsync(
        Guid payoutId,
        [FromBody] UpdatePayoutStatusRequest request,
        AppDbContext db)
    {
        var payout = await db.Payouts.FirstOrDefaultAsync(p => p.Id == payoutId);
        if (payout is null)
            return Results.NotFound(new { error = "Payout not found" });

        var validStatuses = new[] { "pending", "processing", "completed", "failed" };
        if (!validStatuses.Contains(request.Status.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid status" });

        payout.Status = request.Status.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(request.ExternalPayoutId))
            payout.ExternalPayoutId = request.ExternalPayoutId;

        if (!string.IsNullOrWhiteSpace(request.ErrorMessage))
            payout.ErrorMessage = request.ErrorMessage;

        if (request.Status.ToLowerInvariant() == "processing" && !payout.ProcessedAt.HasValue)
            payout.ProcessedAt = DateTime.UtcNow;

        if (request.Status.ToLowerInvariant() == "completed" && !payout.CompletedAt.HasValue)
            payout.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { payout.Id, payout.Status, payout.CompletedAt });
    }

    private static async Task<IResult> ExecutePayoutAsync(
        Guid payoutId,
        [FromBody] ExecutePayoutRequest request,
        AppDbContext db,
        Services.IPayoutExecutionService payoutService,
        INotificationService notifications)
    {
        var payout = await db.Payouts
            .Include(p => p.Owner)
            .ThenInclude(o => o!.OwnerProfile)
            .FirstOrDefaultAsync(p => p.Id == payoutId);

        if (payout is null)
            return Results.NotFound(new { error = "Payout not found" });

        if (payout.Status != "pending")
            return Results.BadRequest(new { error = $"Cannot execute payout with status '{payout.Status}'" });

        // Get payment details from owner profile
        var owner = payout.Owner;
        if (owner?.OwnerProfile == null)
            return Results.BadRequest(new { error = "Owner profile not found" });

        var paymentDetails = new Dictionary<string, string>
        {
            ["amount"] = payout.Amount.ToString("F2")
        };

        // Parse payout details from owner profile JSON
        if (!string.IsNullOrWhiteSpace(owner.OwnerProfile.PayoutDetailsJson))
        {
            try
            {
                var details = JsonSerializer.Deserialize<Dictionary<string, string>>(owner.OwnerProfile.PayoutDetailsJson);
                if (details != null)
                {
                    foreach (var kvp in details)
                        paymentDetails[kvp.Key] = kvp.Value;
                }
            }
            catch { }
        }

        // Add any override details from request
        if (request.PaymentDetails != null)
        {
            foreach (var kvp in request.PaymentDetails)
                paymentDetails[kvp.Key] = kvp.Value;
        }

        // Update payout status to processing
        var oldStatus = payout.Status;
        payout.Status = "processing";
        payout.ProcessedAt = DateTime.UtcNow;

        // Add payout audit log: processing
        try
        {
            db.PayoutAuditLogs.Add(new Models.PayoutAuditLog
            {
                PayoutId = payout.Id,
                Action = "processing",
                OldStatus = oldStatus,
                NewStatus = "processing",
                PerformedByUserId = null,
                Notes = "Processing initiated by admin"
            });
        }
        catch { }

        await db.SaveChangesAsync();

        // Execute the actual payout
        var success = await payoutService.ExecutePayoutAsync(payout.Id, request.PaymentMethod, paymentDetails);

        if (success)
        {
            payout.Status = "completed";
            payout.CompletedAt = DateTime.UtcNow;
            payout.ExternalPayoutId = request.ExternalReference;

            db.PayoutAuditLogs.Add(new Models.PayoutAuditLog
            {
                PayoutId = payout.Id,
                Action = "completed",
                OldStatus = "processing",
                NewStatus = "completed",
                PerformedByUserId = null,
                Notes = "Payout executed successfully"
            });

            // Notify owner about success
            if (payout.Owner != null)
            {
                await notifications.SendOwnerNotificationAsync(payout.Owner, "Payout completed", $"Your payout {payout.Reference} of {payout.Currency} {payout.Amount:F2} has been completed.");
            }
        }
        else
        {
            payout.Status = "failed";
            payout.ErrorMessage = "Payout execution failed";

            db.PayoutAuditLogs.Add(new Models.PayoutAuditLog
            {
                PayoutId = payout.Id,
                Action = "failed",
                OldStatus = "processing",
                NewStatus = "failed",
                PerformedByUserId = null,
                Notes = payout.ErrorMessage
            });

            // Notify owner about failure
            if (payout.Owner != null)
            {
                await notifications.SendOwnerNotificationAsync(payout.Owner, "Payout failed", $"Your payout {payout.Reference} of {payout.Currency} {payout.Amount:F2} failed to execute. Please contact support.");
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            payout.Id,
            payout.Status,
            payout.ProcessedAt,
            payout.CompletedAt,
            payout.ErrorMessage
        });
    }

    // Aggregate documents from vehicles and user profiles so admin UI can fetch them from a single endpoint
    private static async Task<IResult> GetDocumentsAsync(AppDbContext db)
    {
        // Vehicle documents with owner details
        var vehicles = await db.Vehicles.Include(v => v.Owner).ToListAsync();
        var vehicleDocs = new List<object>();
        
        foreach (var v in vehicles)
        {
            if (v.InsuranceDocumentUrl != null)
            {
                vehicleDocs.Add(new {
                    id = $"vehicle-insurance-{v.Id}",
                    documentId = (Guid?)null,
                    type = "vehicle",
                    subType = "insurance",
                    title = "Insurance Document",
                    url = v.InsuranceDocumentUrl,
                    status = "pending",
                    vehicleId = (Guid?)v.Id,
                    vehiclePlateNumber = v.PlateNumber,
                    vehicleMake = v.Make,
                    vehicleModel = v.Model,
                    ownerId = (Guid?)v.OwnerId,
                    ownerName = v.Owner != null ? v.Owner.FirstName + " " + v.Owner.LastName : "Unknown Owner",
                    ownerEmail = v.Owner != null ? v.Owner.Email : null,
                    ownerPhone = v.Owner != null ? v.Owner.Phone : null,
                });
            }
            if (v.RoadworthinessDocumentUrl != null)
            {
                vehicleDocs.Add(new {
                    id = $"vehicle-roadworthiness-{v.Id}",
                    documentId = (Guid?)null,
                    type = "vehicle",
                    subType = "roadworthiness",
                    title = "Roadworthiness Certificate",
                    url = v.RoadworthinessDocumentUrl,
                    status = "pending",
                    vehicleId = (Guid?)v.Id,
                    vehiclePlateNumber = v.PlateNumber,
                    vehicleMake = v.Make,
                    vehicleModel = v.Model,
                    ownerId = (Guid?)v.OwnerId,
                    ownerName = v.Owner != null ? v.Owner.FirstName + " " + v.Owner.LastName : "Unknown Owner",
                    ownerEmail = v.Owner != null ? v.Owner.Email : null,
                    ownerPhone = v.Owner != null ? v.Owner.Phone : null,
                });
            }
        }

        // Renter documents with user details
        var renters = await db.RenterProfiles.Include(r => r.User).ToListAsync();
        var renterDocs = new List<object>();
        
        foreach (var r in renters)
        {
            if (r.DriverLicensePhotoUrl != null)
            {
                renterDocs.Add(new {
                    id = $"renter-license-{r.UserId}",
                    documentId = (Guid?)null,
                    type = "renter",
                    subType = "driver_license",
                    title = "Driver License",
                    url = r.DriverLicensePhotoUrl,
                    status = r.VerificationStatus == "basic_verified" ? "verified" : "pending",
                    userId = (Guid?)r.UserId,
                    userName = r.User != null ? r.User.FirstName + " " + r.User.LastName : null,
                    userEmail = r.User != null ? r.User.Email : null,
                    userPhone = r.User != null ? r.User.Phone : null,
                    licenseNumber = r.DriverLicenseNumber,
                    expiryDate = r.DriverLicenseExpiryDate,
                });
            }
            if (r.NationalIdPhotoUrl != null)
            {
                renterDocs.Add(new {
                    id = $"renter-nationalid-{r.UserId}",
                    documentId = (Guid?)null,
                    type = "renter",
                    subType = "national_id",
                    title = "National ID",
                    url = r.NationalIdPhotoUrl,
                    status = r.VerificationStatus == "basic_verified" ? "verified" : "pending",
                    userId = (Guid?)r.UserId,
                    userName = r.User != null ? r.User.FirstName + " " + r.User.LastName : null,
                    userEmail = r.User != null ? r.User.Email : null,
                    userPhone = r.User != null ? r.User.Phone : null,
                    licenseNumber = (string?)null,
                    expiryDate = (DateTime?)null,
                });
            }
            if (r.PassportPhotoUrl != null)
            {
                renterDocs.Add(new {
                    id = $"renter-passport-{r.UserId}",
                    documentId = (Guid?)null,
                    type = "renter",
                    subType = "passport",
                    title = "Passport",
                    url = r.PassportPhotoUrl,
                    status = r.VerificationStatus == "basic_verified" ? "verified" : "pending",
                    userId = (Guid?)r.UserId,
                    userName = r.User != null ? r.User.FirstName + " " + r.User.LastName : null,
                    userEmail = r.User != null ? r.User.Email : null,
                    userPhone = r.User != null ? r.User.Phone : null,
                    licenseNumber = (string?)null,
                    expiryDate = r.PassportExpiryDate,
                });
            }
        }

        // Driver documents with user details
        var drivers = await db.DriverProfiles.Include(d => d.User).ToListAsync();
        var driverDocs = new List<object>();
        
        foreach (var d in drivers)
        {
            if (d.PhotoUrl != null)
            {
                driverDocs.Add(new {
                    id = $"driver-license-{d.UserId}",
                    documentId = (Guid?)null,
                    type = "driver",
                    subType = "driver_license",
                    title = "Driver License",
                    url = d.PhotoUrl,
                    status = d.VerificationStatus == "verified" ? "verified" : "pending",
                    userId = (Guid?)d.UserId,
                    userName = d.User != null ? d.User.FirstName + " " + d.User.LastName : null,
                    userEmail = d.User != null ? d.User.Email : null,
                    userPhone = d.User != null ? d.User.Phone : null,
                    licenseNumber = d.LicenseNumber,
                    expiryDate = d.LicenseExpiryDate,
                });
            }
        }

        return Results.Ok(new {
            total = vehicleDocs.Count + renterDocs.Count + driverDocs.Count,
            vehicleDocs = vehicleDocs,
            renterDocs = renterDocs,
            driverDocs = driverDocs
        });
    }

    // Verify renter documents
    private static async Task<IResult> VerifyRenterDocumentsAsync(
        Guid userId,
        [FromBody] VerifyDocumentRequest request,
        AppDbContext db)
    {
        var renter = await db.RenterProfiles
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (renter is null)
            return Results.NotFound(new { error = "Renter not found" });

        var approve = request.Approve ?? true;
        
        // Update verification status
        if (approve)
        {
            renter.VerificationStatus = "basic_verified";
        }
        else
        {
            renter.VerificationStatus = "unverified";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            userId = userId,
            verificationStatus = renter.VerificationStatus,
            message = approve ? "Renter documents verified successfully" : "Renter documents rejected"
        });
    }

    // Verify driver documents
    private static async Task<IResult> VerifyDriverDocumentsAsync(
        Guid userId,
        [FromBody] VerifyDocumentRequest request,
        AppDbContext db)
    {
        var driver = await db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (driver is null)
            return Results.NotFound(new { error = "Driver not found" });

        var approve = request.Approve ?? true;
        
        // Update verification status
        if (approve)
        {
            driver.VerificationStatus = "verified";
        }
        else
        {
            driver.VerificationStatus = "rejected";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            userId = userId,
            verificationStatus = driver.VerificationStatus,
            message = approve ? "Driver documents verified successfully" : "Driver documents rejected"
        });
    }

    // Verify renter's driver's license specifically
    private static async Task<IResult> VerifyRenterLicenseAsync(
        Guid renterId,
        [FromBody] RenterActionRequest? request,
        AppDbContext db)
    {
        var renter = await db.RenterProfiles
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == renterId);

        if (renter is null)
            return Results.NotFound(new { error = "Renter not found" });

        // Update verification status to driver_verified (highest level)
        renter.VerificationStatus = "driver_verified";

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            renterId = renterId,
            verificationStatus = renter.VerificationStatus,
            message = "Driver's license verified successfully"
        });
    }

    // Reject renter's driver's license
    private static async Task<IResult> RejectRenterLicenseAsync(
        Guid renterId,
        [FromBody] RenterActionRequest? request,
        AppDbContext db)
    {
        var renter = await db.RenterProfiles
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == renterId);

        if (renter is null)
            return Results.NotFound(new { error = "Renter not found" });

        // Downgrade verification status based on what documents they still have
        // If they have national ID or passport, keep basic_verified, otherwise unverified
        bool hasNationalId = !string.IsNullOrWhiteSpace(renter.NationalIdNumber) && 
                            !string.IsNullOrWhiteSpace(renter.NationalIdPhotoUrl);
        bool hasPassport = !string.IsNullOrWhiteSpace(renter.PassportNumber) && 
                          !string.IsNullOrWhiteSpace(renter.PassportPhotoUrl);

        if (hasNationalId || hasPassport)
        {
            renter.VerificationStatus = "basic_verified";
        }
        else
        {
            renter.VerificationStatus = "unverified";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            renterId = renterId,
            verificationStatus = renter.VerificationStatus,
            message = "Driver's license rejected",
            reason = request?.Reason ?? "No reason provided"
        });
    }

    // Suspend a renter account
    private static async Task<IResult> SuspendRenterAsync(
        Guid renterId,
        [FromBody] RenterActionRequest? request,
        AppDbContext db)
    {
        var renter = await db.RenterProfiles
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == renterId);

        if (renter is null)
            return Results.NotFound(new { error = "Renter not found" });

        if (renter.User is null)
            return Results.NotFound(new { error = "User account not found" });

        // Update user status to suspended
        renter.User.Status = "suspended";

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            renterId = renterId,
            userId = renter.User.Id,
            status = renter.User.Status,
            message = "Renter account suspended successfully",
            reason = request?.Reason ?? "No reason provided"
        });
    }

    private static async Task<IResult> GetAppConfigAsync(AppDbContext db, [FromQuery] bool showSensitive = false)
    {
        var configs = await db.AppConfigs.ToListAsync();
        var result = configs.Select(c => new
        {
            c.ConfigKey,
            c.Scope,
            Value = c.IsSensitive && !showSensitive ? null : c.ConfigValue,
            c.IsSensitive,
            c.UpdatedAt
        });

        return Results.Ok(result);
    }

    private static async Task<IResult> UpsertAppConfigAsync(
        [FromBody] AppConfig config,
        AppDbContext db)
    {
        var existing = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == config.ConfigKey);
        if (existing is null)
        {
            // ensure timestamps are set
            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            db.AppConfigs.Add(config);
        }
        else
        {
            existing.Scope = config.Scope;
            existing.ConfigValue = config.ConfigValue;
            existing.IsSensitive = config.IsSensitive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return Results.Ok(config);
    }

    // Get grouped config values (e.g., all Email:Smtp:* keys)
    private static async Task<IResult> GetAppConfigGroupAsync(
        string prefix,
        IAppConfigService configService)
    {
        var group = await configService.GetConfigGroupAsync(prefix);
        return Results.Ok(group);
    }

    public class JwtSettingsRequest
    {
        public string? SigningKeyId { get; set; }
        public int? TokenLifetimeMinutes { get; set; }
    }

    private static async Task<IResult> UpdateJwtSettingsAsync(
        [FromBody] JwtSettingsRequest request,
        AppDbContext db)
    {
        var key = "JwtSettings";
        var existing = await db.AppConfigs.FirstOrDefaultAsync(c => c.ConfigKey == key);
        var payload = new
        {
            signingKeyId = request.SigningKeyId,
            tokenLifetimeMinutes = request.TokenLifetimeMinutes
        };

        var json = JsonSerializer.Serialize(payload);

        if (existing is null)
        {
            existing = new AppConfig
            {
                ConfigKey = key,
                Scope = "security",
                ConfigValue = json
            };
            db.AppConfigs.Add(existing);
        }
        else
        {
            existing.ConfigValue = json;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    // Get app domain configuration
    private static async Task<IResult> GetAppDomainAsync(AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "app_domain");
        
        if (setting is null)
        {
            return Results.Ok(new
            {
                domain = "ryverental.com",
                isDefault = true
            });
        }

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(setting.ValueJson);
            return Results.Ok(new
            {
                domain = data?.GetValueOrDefault("domain", "ryverental.com") ?? "ryverental.com",
                isDefault = false
            });
        }
        catch
        {
            return Results.Ok(new
            {
                domain = "ryverental.com",
                isDefault = true
            });
        }
    }

    // Update app domain configuration
    private static async Task<IResult> UpdateAppDomainAsync(
        [FromBody] UpdateAppDomainRequest request,
        AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return Results.BadRequest(new { error = "Domain is required" });

        // Basic domain validation
        var domain = request.Domain.Trim().ToLowerInvariant();
        if (domain.StartsWith("http://") || domain.StartsWith("https://"))
        {
            return Results.BadRequest(new { error = "Domain should not include protocol (http:// or https://)" });
        }

        var domainData = new Dictionary<string, string>
        {
            { "domain", domain }
        };
        var json = JsonSerializer.Serialize(domainData);

        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "app_domain");
        if (setting is null)
        {
            setting = new GlobalSetting
            {
                Key = "app_domain",
                ValueJson = json
            };
            db.GlobalSettings.Add(setting);
        }
        else
        {
            setting.ValueJson = json;
        }

        await db.SaveChangesAsync();
        
        return Results.Ok(new
        {
            success = true,
            domain = domain,
            message = $"App domain updated to {domain}. Password reset emails will use https://{domain}/reset-password"
        });
    }

    private static async Task<IResult> GetCitiesAsync(AppDbContext db)
    {
        var cities = await db.Cities.OrderBy(c => c.DisplayOrder).ToListAsync();
        return Results.Ok(cities);
    }

    private static async Task<IResult> CreateCityAsync(
        [FromBody] City city,
        AppDbContext db)
    {
        if (city.Id == Guid.Empty)
        {
            city.Id = Guid.NewGuid();
        }

        city.CreatedAt = DateTime.UtcNow;
        city.UpdatedAt = DateTime.UtcNow;

        db.Cities.Add(city);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/cities/{city.Id}", city);
    }

    private static async Task<IResult> DeactivateCityAsync(
        Guid cityId,
        AppDbContext db)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == cityId);
        if (city is null)
            return Results.NotFound(new { error = "City not found" });

        city.IsActive = false;
        city.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"City '{city.Name}' deactivated successfully", city });
    }

    private static async Task<IResult> DeleteCityAsync(
        Guid cityId,
        AppDbContext db)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == cityId);
        if (city is null)
            return Results.NotFound(new { error = "City not found" });

        // Check if city has vehicles
        var hasVehicles = await db.Vehicles.AnyAsync(v => v.CityId == cityId);
        if (hasVehicles)
            return Results.BadRequest(new { error = "Cannot delete city with existing vehicles. Deactivate instead." });

        db.Cities.Remove(city);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"City '{city.Name}' deleted successfully" });
    }

    // Analytics & Metrics Endpoints
    private static async Task<IResult> GetMetricsOverviewAsync(AppDbContext db)
    {
        var totalUsers = await db.Users.CountAsync();
        var totalRenters = await db.Users.CountAsync(u => u.Role == "renter");
        var totalOwners = await db.Users.CountAsync(u => u.Role == "owner");
        var activeOwners = await db.Users.CountAsync(u => u.Role == "owner" && u.Status == "active");
        var totalVehicles = await db.Vehicles.CountAsync();
        var activeVehicles = await db.Vehicles.CountAsync(v => v.Status == "active");
        var totalBookings = await db.Bookings.CountAsync();
        var completedBookings = await db.Bookings.CountAsync(b => b.Status == "completed");
        var activeBookings = await db.Bookings.CountAsync(b => b.Status == "confirmed" || b.Status == "active");
        
        var totalRevenue = await db.Bookings
            .Where(b => b.Status == "completed")
            .SumAsync(b => b.TotalAmount);

        var last30Days = DateTime.UtcNow.AddDays(-30);
        var recentBookings = await db.Bookings
            .Where(b => b.CreatedAt >= last30Days)
            .CountAsync();
        var recentRevenue = await db.Bookings
            .Where(b => b.CreatedAt >= last30Days && b.Status == "completed")
            .SumAsync(b => b.TotalAmount);

        return Results.Ok(new
        {
            users = new { total = totalUsers, renters = totalRenters, owners = totalOwners, activeOwners },
            vehicles = new { total = totalVehicles, active = activeVehicles },
            bookings = new { total = totalBookings, completed = completedBookings, active = activeBookings },
            revenue = new { total = totalRevenue, last30Days = recentRevenue },
            activity = new { bookingsLast30Days = recentBookings }
        });
    }

    private static async Task<IResult> GetBookingMetricsAsync(
        AppDbContext db,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var bookings = await db.Bookings
            .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
            .ToListAsync();

        var byStatus = bookings
            .GroupBy(b => b.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToList();

        var byDay = bookings
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        var avgBookingValue = bookings.Any() ? bookings.Average(b => b.TotalAmount) : 0;

        return Results.Ok(new
        {
            period = new { from = fromDate, to = toDate },
            total = bookings.Count,
            byStatus,
            byDay,
            avgBookingValue,
            completionRate = bookings.Any() 
                ? (double)bookings.Count(b => b.Status == "completed") / bookings.Count * 100 
                : 0
        });
    }

    private static async Task<IResult> GetRevenueMetricsAsync(
        AppDbContext db,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var bookings = await db.Bookings
            .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
            .ToListAsync();

        var payments = await db.PaymentTransactions
            .Where(p => p.Type == "payment" && p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .ToListAsync();

        // Completed bookings revenue
        var completedBookings = bookings.Where(b => b.Status == "completed").ToList();
        var totalRevenue = completedBookings.Sum(b => b.TotalAmount);
        
        // Separate platform revenues (admin keeps these)
        var protectionPlanRevenue = completedBookings.Sum(b => b.ProtectionAmount ?? 0);
        var platformFeeRevenue = completedBookings.Sum(b => b.PlatformFee ?? 0);
        var insuranceRevenue = completedBookings.Sum(b => b.InsuranceAmount ?? 0);
        var depositRevenue = completedBookings.Sum(b => b.DepositAmount);
        
        // Owner revenue (rental + driver fees - platform commission)
        var ownerRevenue = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        // Paid vs Pending/Expected
        var paidBookings = bookings.Where(b => payments.Any(p => p.BookingId == b.Id && p.Status == "completed")).ToList();
        var totalPaid = paidBookings.Sum(b => b.TotalAmount);
        var paidProtectionPlan = paidBookings.Sum(b => b.ProtectionAmount ?? 0);
        var paidPlatformFee = paidBookings.Sum(b => b.PlatformFee ?? 0);
        var paidInsurance = paidBookings.Sum(b => b.InsuranceAmount ?? 0);
        var paidDeposits = paidBookings.Sum(b => b.DepositAmount);
        // Owner receives: rental + driver fees - platform commission
        var paidToOwners = paidBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        var pendingBookings = bookings.Where(b => 
            (b.Status == "confirmed" || b.Status == "active") && 
            !payments.Any(p => p.BookingId == b.Id && p.Status == "completed")).ToList();
        var expectedRevenue = pendingBookings.Sum(b => b.TotalAmount);
        var expectedProtectionPlan = pendingBookings.Sum(b => b.ProtectionAmount ?? 0);
        var expectedPlatformFee = pendingBookings.Sum(b => b.PlatformFee ?? 0);
        var expectedInsurance = pendingBookings.Sum(b => b.InsuranceAmount ?? 0);
        // Owner receives: rental + driver fees - platform commission
        var expectedOwnerRevenue = pendingBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        var revenueByDay = completedBookings
            .GroupBy(b => b.CreatedAt.Date)
            .Select(g => new 
            { 
                date = g.Key, 
                totalRevenue = g.Sum(b => b.TotalAmount),
                protectionRevenue = g.Sum(b => b.ProtectionAmount ?? 0),
                platformFeeRevenue = g.Sum(b => b.PlatformFee ?? 0),
                insuranceRevenue = g.Sum(b => b.InsuranceAmount ?? 0),
                // Owner receives: rental + driver fees - platform commission
                ownerRevenue = g.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0)),
                bookings = g.Count() 
            })
            .OrderBy(x => x.date)
            .ToList();

        var revenueByMonth = completedBookings
            .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
            .Select(g => new 
            { 
                year = g.Key.Year, 
                month = g.Key.Month, 
                totalRevenue = g.Sum(b => b.TotalAmount),
                protectionRevenue = g.Sum(b => b.ProtectionAmount ?? 0),
                platformFeeRevenue = g.Sum(b => b.PlatformFee ?? 0),
                insuranceRevenue = g.Sum(b => b.InsuranceAmount ?? 0),
                // Owner receives: rental + driver fees - platform commission
                ownerRevenue = g.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0)),
                bookings = g.Count() 
            })
            .OrderBy(x => x.year).ThenBy(x => x.month)
            .ToList();

        var totalBookings = completedBookings.Count;
        var avgRevenuePerBooking = totalBookings > 0 ? totalRevenue / totalBookings : 0;

        return Results.Ok(new
        {
            period = new { from = fromDate, to = toDate },
            summary = new 
            {
                totalRevenue,
                protectionPlanRevenue, // Platform keeps this
                ownerRevenue, // Amount owed to owners
                totalBookings,
                avgRevenuePerBooking
            },
            paymentStatus = new
            {
                totalPaid,
                paidProtectionPlan,
                paidToOwners,
                expectedRevenue,
                expectedProtectionPlan,
                expectedOwnerRevenue,
                pendingBookingsCount = pendingBookings.Count
            },
            revenueByDay,
            revenueByMonth
        });
    }

    private static async Task<IResult> GetTopOwnersAsync(
        AppDbContext db,
        [FromQuery] int limit = 10)
    {
        var topOwners = await db.Users
            .Where(u => u.Role == "owner")
            .Select(u => new
            {
                ownerId = u.Id,
                ownerEmail = u.Email,
                ownerName = $"{u.FirstName} {u.LastName}",
                vehicleCount = db.Vehicles.Count(v => v.OwnerId == u.Id),
                activeVehicles = db.Vehicles.Count(v => v.OwnerId == u.Id && v.Status == "active"),
                totalBookings = db.Bookings.Count(b => b.Vehicle != null && b.Vehicle.OwnerId == u.Id),
                completedBookings = db.Bookings.Count(b => b.Vehicle != null && b.Vehicle.OwnerId == u.Id && b.Status == "completed"),
                totalRevenue = db.Bookings
                    .Where(b => b.Vehicle != null && b.Vehicle.OwnerId == u.Id && b.Status == "completed")
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0
            })
            .OrderByDescending(x => x.totalRevenue)
            .Take(limit)
            .ToListAsync();

        return Results.Ok(new { topOwners });
    }

    private static async Task<IResult> GetTopVehiclesAsync(
        AppDbContext db,
        [FromQuery] int limit = 10)
    {
        var topVehicles = await db.Vehicles
            .Where(v => v.Status == "active")
            .Select(v => new
            {
                vehicleId = v.Id,
                make = v.Make,
                model = v.Model,
                year = v.Year,
                category = v.Category,
                plateNumber = v.PlateNumber,
                ownerEmail = v.Owner != null ? v.Owner.Email : null,
                bookingCount = db.Bookings.Count(b => b.VehicleId == v.Id),
                completedBookings = db.Bookings.Count(b => b.VehicleId == v.Id && b.Status == "completed"),
                totalRevenue = db.Bookings
                    .Where(b => b.VehicleId == v.Id && b.Status == "completed")
                    .Sum(b => (decimal?)b.TotalAmount) ?? 0
            })
            .OrderByDescending(x => x.totalRevenue)
            .Take(limit)
            .ToListAsync();

        return Results.Ok(new { topVehicles });
    }

    private static async Task<IResult> GetOwnerPayoutsAsync(
        AppDbContext db,
        [FromQuery] string? status = "completed") // completed | all
    {
        var query = db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Owner)
            .AsQueryable();

        if (status == "completed")
        {
            query = query.Where(b => b.Status == "completed");
        }

        var bookings = await query.ToListAsync();

        var ownerPayouts = bookings
            .Where(b => b.Vehicle != null && b.Vehicle.Owner != null)
            .GroupBy(b => b.Vehicle!.OwnerId)
            .Select(g => new
            {
                ownerId = g.Key,
                ownerEmail = g.First().Vehicle!.Owner!.Email,
                ownerName = $"{g.First().Vehicle!.Owner!.FirstName} {g.First().Vehicle!.Owner!.LastName}",
                totalBookings = g.Count(),
                totalGrossRevenue = g.Sum(b => b.TotalAmount),
                totalProtectionPlanRevenue = g.Sum(b => b.ProtectionAmount ?? 0),
                totalPlatformFeeRevenue = g.Sum(b => b.PlatformFee ?? 0),
                totalInsuranceRevenue = g.Sum(b => b.InsuranceAmount ?? 0),
                // Owner receives: rental + driver fees - platform commission
                totalOwnerRevenue = g.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0)),
                totalDriverFees = g.Sum(b => b.DriverAmount ?? 0), // Tracked separately for accounting
                vehicles = g.GroupBy(b => b.VehicleId).Select(vg => new
                {
                    vehicleId = vg.Key,
                    make = vg.First().Vehicle!.Make,
                    model = vg.First().Vehicle!.Model,
                    plateNumber = vg.First().Vehicle!.PlateNumber,
                    bookings = vg.Count(),
                    grossRevenue = vg.Sum(b => b.TotalAmount),
                    protectionRevenue = vg.Sum(b => b.ProtectionAmount ?? 0),
                    platformFeeRevenue = vg.Sum(b => b.PlatformFee ?? 0),
                    insuranceRevenue = vg.Sum(b => b.InsuranceAmount ?? 0),
                    // Owner receives: rental + driver fees - platform commission
                    ownerRevenue = vg.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0)),
                    driverFees = vg.Sum(b => b.DriverAmount ?? 0) // Tracked separately
                }).ToList()
            })
            .OrderByDescending(x => x.totalOwnerRevenue)
            .ToList();

        var summary = new
        {
            totalOwners = ownerPayouts.Count,
            totalBookings = bookings.Count,
            totalGrossRevenue = ownerPayouts.Sum(o => o.totalGrossRevenue),
            totalPlatformRevenue = ownerPayouts.Sum(o => o.totalProtectionPlanRevenue),
            totalOwnerPayouts = ownerPayouts.Sum(o => o.totalOwnerRevenue)
        };

        return Results.Ok(new { summary, owners = ownerPayouts });
    }

    private static async Task<IResult> GetCityAnalyticsAsync(AppDbContext db)
    {
        // Get all cities
        var cities = await db.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var cityAnalytics = new List<object>();

        foreach (var city in cities)
        {
            // Get vehicles in this city
            var vehicles = await db.Vehicles
                .Where(v => v.CityId == city.Id)
                .ToListAsync();

            var vehicleIds = vehicles.Select(v => v.Id).ToList();

            // Get bookings for vehicles in this city
            var bookings = await db.Bookings
                .Where(b => vehicleIds.Contains(b.VehicleId))
                .ToListAsync();

            var completedBookings = bookings.Where(b => b.Status == "completed").ToList();

            cityAnalytics.Add(new
            {
                cityId = city.Id,
                cityName = city.Name,
                totalVehicles = vehicles.Count,
                activeVehicles = vehicles.Count(v => v.Status == "active"),
                totalBookings = bookings.Count,
                completedBookings = completedBookings.Count,
                totalRevenue = completedBookings.Sum(b => b.TotalAmount),
                averageBookingValue = completedBookings.Any() 
                    ? completedBookings.Average(b => b.TotalAmount) 
                    : 0
            });
        }

        return Results.Ok(new { cities = cityAnalytics });
    }

    private static async Task<IResult> GetOwnerVerificationsAsync(
        AppDbContext db,
        [FromQuery] string? status = "pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Users
            .Include(u => u.OwnerProfile)
            .Where(u => u.Role == "owner");

        if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            // Include both: new owner accounts (User.Status == "pending") 
            // AND existing owners with pending verification changes
            query = query.Where(u => u.Status == "pending" ||
                (u.OwnerProfile != null && 
                 (u.OwnerProfile.CompanyVerificationStatus == "pending" || 
                  u.OwnerProfile.PayoutVerificationStatus == "pending")));
        }

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var result = items.Select(u => new
        {
            u.Id,
            u.Email,
            u.Phone,
            u.FirstName,
            u.LastName,
            u.Status,
            companyVerification = u.OwnerProfile?.CompanyVerificationStatus,
            payoutVerification = u.OwnerProfile?.PayoutVerificationStatus,
            payoutDetailsPending = u.OwnerProfile?.PayoutDetailsPendingJson
        });

        return Results.Ok(new { total, page, pageSize, data = result });
    }

    public record VerificationActionRequest(string Type, bool Approve, string? Notes);

    private static async Task<IResult> ApproveOwnerVerificationAsync(
        Guid userId,
        [FromBody] VerificationActionRequest request,
        AppDbContext db)
    {
        var user = await db.Users.Include(u => u.OwnerProfile).FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");
        if (user is null)
            return Results.NotFound(new { error = "Owner not found" });

        // Handle account approval (for new pending accounts)
        if (request.Type == "account" || string.IsNullOrEmpty(request.Type))
        {
            if (user.Status != "pending")
                return Results.BadRequest(new { error = "Account is not pending approval" });

            user.Status = request.Approve ? "active" : "rejected";
            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
            { 
                UserId = userId, 
                Field = "AccountStatus", 
                OldValue = "pending", 
                NewValue = user.Status, 
                ChangedByUserId = null 
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, status = user.Status });
        }

        // Profile verification changes (requires OwnerProfile)
        if (user.OwnerProfile is null)
            return Results.NotFound(new { error = "Owner profile not found" });

        if (request.Type == "company")
        {
            if (user.OwnerProfile.CompanyVerificationStatus != "pending")
                return Results.BadRequest(new { error = "No pending company verification" });

            user.OwnerProfile.CompanyVerificationStatus = request.Approve ? "verified" : "rejected";
            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit { UserId = userId, Field = "CompanyVerification", OldValue = null, NewValue = user.OwnerProfile.CompanyVerificationStatus, ChangedByUserId = null });
        }
        else if (request.Type == "payout")
        {
            if (user.OwnerProfile.PayoutVerificationStatus != "pending")
                return Results.BadRequest(new { error = "No pending payout verification" });

            if (request.Approve)
            {
                user.OwnerProfile.PayoutDetailsJson = user.OwnerProfile.PayoutDetailsPendingJson;
                user.OwnerProfile.PayoutDetailsPendingJson = null;
                user.OwnerProfile.PayoutVerificationStatus = "verified";
            }
            else
            {
                user.OwnerProfile.PayoutDetailsPendingJson = null;
                user.OwnerProfile.PayoutVerificationStatus = "rejected";
            }

            await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit { UserId = userId, Field = "PayoutVerification", OldValue = null, NewValue = user.OwnerProfile.PayoutVerificationStatus, ChangedByUserId = null });
        }
        else
        {
            return Results.BadRequest(new { error = "Unknown verification type" });
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> RejectOwnerVerificationAsync(
        Guid userId,
        [FromBody] VerificationActionRequest request,
        AppDbContext db)
    {
        // Reuse approval with Approve=false behavior
        request = request with { Approve = false };
        return await ApproveOwnerVerificationAsync(userId, request, db);
    }

    private static async Task<IResult> GetOwnerPayoutDetailsAsync(
        Guid userId,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner?.OwnerProfile == null)
            return Results.NotFound(new { error = "Owner not found" });

        return Results.Ok(new
        {
            userId = owner.Id,
            ownerName = $"{owner.FirstName} {owner.LastName}",
            email = owner.Email,
            payoutPreference = owner.OwnerProfile.PayoutPreference,
            payoutDetails = PayoutDetailsDto.Parse(owner.OwnerProfile.PayoutDetailsJson),
            payoutDetailsPending = PayoutDetailsDto.Parse(owner.OwnerProfile.PayoutDetailsPendingJson),
            payoutVerificationStatus = owner.OwnerProfile.PayoutVerificationStatus,
            payoutFrequency = owner.OwnerProfile.PayoutFrequency,
            minimumPayoutAmount = owner.OwnerProfile.MinimumPayoutAmount,
            instantWithdrawalEnabled = owner.OwnerProfile.InstantWithdrawalEnabled
        });
    }

    private static async Task<IResult> VerifyOwnerPayoutMethodAsync(
        Guid userId,
        [FromBody] VerifyPayoutMethodRequest? request,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner?.OwnerProfile == null)
            return Results.NotFound(new { error = "Owner not found" });

        if (owner.OwnerProfile.PayoutVerificationStatus != "pending")
            return Results.BadRequest(new { error = "No pending payout verification" });

        if (string.IsNullOrWhiteSpace(owner.OwnerProfile.PayoutDetailsPendingJson))
            return Results.BadRequest(new { error = "No pending payout details to verify" });

        // Approve: move pending to active
        owner.OwnerProfile.PayoutDetailsJson = owner.OwnerProfile.PayoutDetailsPendingJson;
        owner.OwnerProfile.PayoutDetailsPendingJson = null;
        owner.OwnerProfile.PayoutVerificationStatus = "verified";

        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "PayoutMethodVerification", 
            OldValue = "pending", 
            NewValue = "verified", 
            ChangedByUserId = null 
        });

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            success = true, 
            message = "Payout method verified successfully",
            payoutDetails = PayoutDetailsDto.Parse(owner.OwnerProfile.PayoutDetailsJson)
        });
    }

    private static async Task<IResult> RejectOwnerPayoutMethodAsync(
        Guid userId,
        [FromBody] VerifyPayoutMethodRequest request,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner?.OwnerProfile == null)
            return Results.NotFound(new { error = "Owner not found" });

        if (owner.OwnerProfile.PayoutVerificationStatus != "pending")
            return Results.BadRequest(new { error = "No pending payout verification" });

        // Reject: clear pending details
        owner.OwnerProfile.PayoutDetailsPendingJson = null;
        owner.OwnerProfile.PayoutVerificationStatus = owner.OwnerProfile.PayoutDetailsJson == null ? "unverified" : "verified";

        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "PayoutMethodVerification", 
            OldValue = "pending", 
            NewValue = "rejected", 
            ChangedByUserId = null
        });

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            success = true, 
            message = $"Payout method rejected{(string.IsNullOrWhiteSpace(request.Reason) ? "" : ": " + request.Reason)}"
        });
    }

    // Owner CRUD operations
    private static async Task<IResult> GetOwnersAsync(
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Users
            .AsNoTracking()
            .Where(u => u.Role == "owner");

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status.ToLowerInvariant());

        var totalCount = await query.CountAsync();
        
        // Get owner IDs for this page
        var ownerIds = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u.Id)
            .ToListAsync();

        // Get users
        var owners = await db.Users
            .AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new
            {
                userId = u.Id,
                email = u.Email,
                phone = u.Phone,
                firstName = u.FirstName,
                lastName = u.LastName,
                status = u.Status,
                createdAt = u.CreatedAt
            })
            .ToListAsync();

        // Get owner profiles
        var profiles = await db.OwnerProfiles
            .AsNoTracking()
            .Where(op => ownerIds.Contains(op.UserId))
            .Select(op => new
            {
                userId = op.UserId,
                ownerType = op.OwnerType,
                displayName = op.DisplayName,
                companyName = op.CompanyName,
                businessRegistrationNumber = op.BusinessRegistrationNumber,
                payoutPreference = op.PayoutPreference,
                companyVerificationStatus = op.CompanyVerificationStatus,
                payoutVerificationStatus = op.PayoutVerificationStatus,
                payoutDetailsPending = op.PayoutVerificationStatus == "pending" 
                    ? PayoutDetailsDto.Parse(op.PayoutDetailsPendingJson) 
                    : null
            })
            .ToListAsync();

        // Get vehicle counts
        var vehicleCounts = await db.Vehicles
            .AsNoTracking()
            .Where(v => ownerIds.Contains(v.OwnerId))
            .GroupBy(v => v.OwnerId)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Combine in memory
        var result = owners.Select(o => new
        {
            o.userId,
            o.email,
            o.phone,
            o.firstName,
            o.lastName,
            o.status,
            o.createdAt,
            ownerProfile = profiles.FirstOrDefault(p => p.userId == o.userId),
            vehicleCount = vehicleCounts.FirstOrDefault(vc => vc.OwnerId == o.userId)?.Count ?? 0
        }).ToList();

        return Results.Ok(new { totalCount, page, pageSize, data = result });
    }

    private static async Task<IResult> GetOwnerDetailsAsync(
        Guid userId,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner is null)
            return Results.NotFound(new { error = "Owner not found" });

        var vehicles = await db.Vehicles
            .Where(v => v.OwnerId == userId)
            .Select(v => new { v.Id, v.Make, v.Model, v.Year, v.PlateNumber, v.Status })
            .ToListAsync();

        return Results.Ok(new
        {
            userId = owner.Id,
            email = owner.Email,
            phone = owner.Phone,
            firstName = owner.FirstName,
            lastName = owner.LastName,
            status = owner.Status,
            phoneVerified = owner.PhoneVerified,
            createdAt = owner.CreatedAt,
            ownerProfile = owner.OwnerProfile == null ? null : new
            {
                ownerType = owner.OwnerProfile.OwnerType,
                displayName = owner.OwnerProfile.DisplayName,
                companyName = owner.OwnerProfile.CompanyName,
                businessRegistrationNumber = owner.OwnerProfile.BusinessRegistrationNumber,
                payoutPreference = owner.OwnerProfile.PayoutPreference,
                payoutDetails = PayoutDetailsDto.Parse(owner.OwnerProfile.PayoutDetailsJson),
                payoutDetailsPending = PayoutDetailsDto.Parse(owner.OwnerProfile.PayoutDetailsPendingJson),
                payoutVerificationStatus = owner.OwnerProfile.PayoutVerificationStatus,
                companyVerificationStatus = owner.OwnerProfile.CompanyVerificationStatus,
                payoutFrequency = owner.OwnerProfile.PayoutFrequency,
                minimumPayoutAmount = owner.OwnerProfile.MinimumPayoutAmount,
                instantWithdrawalEnabled = owner.OwnerProfile.InstantWithdrawalEnabled
            },
            vehicles
        });
    }

    private static async Task<IResult> UpdateOwnerAsync(
        Guid userId,
        [FromBody] UpdateOwnerRequest request,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner is null)
            return Results.NotFound(new { error = "Owner not found" });

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            owner.FirstName = request.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(request.LastName))
            owner.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Phone))
            owner.Phone = request.Phone.Trim();

        if (!string.IsNullOrWhiteSpace(request.Status))
            owner.Status = request.Status.ToLowerInvariant();

        if (owner.OwnerProfile != null)
        {
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
                owner.OwnerProfile.DisplayName = request.DisplayName.Trim();

            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                owner.OwnerProfile.CompanyName = request.CompanyName.Trim();
        }

        try
        {
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true, message = "Owner updated successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem("Could not update owner: " + ex.Message);
        }
    }

    private static async Task<IResult> DeleteOwnerAsync(
        Guid userId,
        AppDbContext db)
    {
        var owner = await db.Users
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");

        if (owner is null)
            return Results.NotFound(new { error = "Owner not found" });

        // Check if owner has vehicles
        var hasVehicles = await db.Vehicles.AnyAsync(v => v.OwnerId == userId);
        if (hasVehicles)
            return Results.BadRequest(new { error = "Cannot delete owner with vehicles. Please remove all vehicles first." });

        // Check if owner has active bookings
        var hasActiveBookings = await db.Bookings
            .AnyAsync(b => b.Vehicle != null && b.Vehicle.OwnerId == userId && 
                          (b.Status == "confirmed" || b.Status == "active"));
        if (hasActiveBookings)
            return Results.BadRequest(new { error = "Cannot delete owner with active bookings." });

        if (owner.OwnerProfile != null)
            db.OwnerProfiles.Remove(owner.OwnerProfile);

        db.Users.Remove(owner);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = $"Owner '{owner.FirstName} {owner.LastName}' deleted successfully" });
    }

    private static async Task<IResult> DeleteVehicleAsync(
        Guid vehicleId,
        AppDbContext db,
        IFileUploadService fileUploadService)
    {
        // Just delegate to force delete - admin can delete anything
        return await ForceDeleteVehicleAsync(vehicleId, db, fileUploadService);
    }

    private static async Task<IResult> ForceDeleteVehicleAsync(
        Guid vehicleId,
        AppDbContext db,
        IFileUploadService fileUploadService)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
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

            // Step 6: Delete vehicle photos
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
            
            // Step 7: Delete insurance document
            if (!string.IsNullOrWhiteSpace(vehicle.InsuranceDocumentUrl))
            {
                if (await fileUploadService.DeleteFileAsync(vehicle.InsuranceDocumentUrl))
                    deletedFiles++;
            }
            
            // Step 8: Delete roadworthiness document
            if (!string.IsNullOrWhiteSpace(vehicle.RoadworthinessDocumentUrl))
            {
                if (await fileUploadService.DeleteFileAsync(vehicle.RoadworthinessDocumentUrl))
                    deletedFiles++;
            }

            // Step 9: Reload and delete the vehicle (was cleared from tracker)
            var vehicleToDelete = await db.Vehicles.FindAsync(vehicleId);
            if (vehicleToDelete != null)
            {
                db.Vehicles.Remove(vehicleToDelete);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { 
                success = true, 
                message = $"Vehicle '{vehicle.Make} {vehicle.Model}' ({vehicle.PlateNumber}) forcefully deleted", 
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

    // Owner account management endpoints
    private static async Task<IResult> ApproveOwnerAccountAsync(
        Guid userId,
        AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");
        if (user is null)
            return Results.NotFound(new { error = "Owner not found" });

        if (user.Status != "pending")
            return Results.BadRequest(new { error = $"Cannot approve owner with status '{user.Status}'" });

        user.Status = "active";
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "AccountStatus", 
            OldValue = "pending", 
            NewValue = "active", 
            ChangedByUserId = null 
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Owner account approved and activated", status = "active" });
    }

    private static async Task<IResult> RejectOwnerAccountAsync(
        Guid userId,
        [FromBody] RejectAccountRequest? request,
        AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");
        if (user is null)
            return Results.NotFound(new { error = "Owner not found" });

        if (user.Status != "pending")
            return Results.BadRequest(new { error = $"Cannot reject owner with status '{user.Status}'" });

        user.Status = "rejected";
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "AccountStatus", 
            OldValue = "pending", 
            NewValue = request?.Reason ?? "rejected",
            ChangedByUserId = null 
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Owner account rejected", status = "rejected" });
    }

    private static async Task<IResult> ActivateOwnerAccountAsync(
        Guid userId,
        AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");
        if (user is null)
            return Results.NotFound(new { error = "Owner not found" });

        var oldStatus = user.Status;
        user.Status = "active";
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "AccountStatus", 
            OldValue = oldStatus, 
            NewValue = "active", 
            ChangedByUserId = null 
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Owner account activated", status = "active" });
    }

    private static async Task<IResult> DeactivateOwnerAccountAsync(
        Guid userId,
        [FromBody] RejectAccountRequest? request,
        AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.Role == "owner");
        if (user is null)
            return Results.NotFound(new { error = "Owner not found" });

        var oldStatus = user.Status;
        user.Status = "suspended";
        await db.ProfileChangeAudits.AddAsync(new ProfileChangeAudit 
        { 
            UserId = userId, 
            Field = "AccountStatus", 
            OldValue = oldStatus, 
            NewValue = request?.Reason ?? "suspended",
            ChangedByUserId = null 
        });
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Owner account deactivated", status = "suspended" });
    }

    // Partner application management
    private static async Task<IResult> GetPartnerApplicationsAsync(
        AppDbContext db,
        [FromQuery] string? status = "pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Partners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                // Pending applications are inactive and not verified
                query = query.Where(p => !p.IsActive && !p.IsVerified);
            }
            else if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.IsActive && p.IsVerified);
            }
            else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            {
                // Rejected partners are inactive but were previously reviewed
                query = query.Where(p => !p.IsActive && p.IsVerified);
            }
        }

        var total = await query.CountAsync();
        var partners = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.City,
                p.Country,
                p.PhoneNumber,
                p.WebsiteUrl,
                p.LogoUrl,
                p.IsActive,
                p.IsVerified,
                p.IsFeatured,
                targetRoles = p.TargetRoles,
                categories = p.Categories,
                p.CreatedAt
            })
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = partners });
    }

    private static async Task<IResult> ApprovePartnerApplicationAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        partner.IsActive = true;
        partner.IsVerified = true;
        partner.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = $"Partner '{partner.Name}' approved", isActive = true, isVerified = true });
    }

    private static async Task<IResult> RejectPartnerApplicationAsync(
        Guid partnerId,
        [FromBody] RejectAccountRequest? request,
        AppDbContext db)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        partner.IsActive = false;
        partner.IsVerified = false;
        partner.UpdatedAt = DateTime.UtcNow;
        // Store rejection reason in metadata if provided
        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            var metadata = string.IsNullOrWhiteSpace(partner.Metadata) 
                ? new Dictionary<string, string>() 
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(partner.Metadata) ?? new Dictionary<string, string>();
            metadata["rejectionReason"] = request.Reason;
            metadata["rejectedAt"] = DateTime.UtcNow.ToString("O");
            partner.Metadata = System.Text.Json.JsonSerializer.Serialize(metadata);
        }
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = $"Partner '{partner.Name}' rejected", isActive = false });
    }

public record UpdateAppDomainRequest(string Domain);

public record ExecutePayoutRequest(
    string PaymentMethod,
    string? ExternalReference,
    Dictionary<string, string>? PaymentDetails
);

public record RejectAccountRequest(string? Reason);
private static async Task<IResult> SendTestEmailAsync(
        [FromBody] TestEmailRequest request,
        IEmailService emailService,
        IAppConfigService configService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AdminEndpoints");
        var contact = !string.IsNullOrWhiteSpace(request.EmailOrPhone) ? request.EmailOrPhone : request.ToEmail;
        if (string.IsNullOrWhiteSpace(contact))
            return Results.BadRequest(new { error = "ToEmail or EmailOrPhone is required" });

        // If contact looks like an email, try sending via configured providers (Postmark, Azure, SMTP)
        if (contact.Contains("@"))
        {
            var postmarkKey = await configService.GetConfigValueAsync("Email:Postmark:ApiKey");
            var smtpHost = await configService.GetConfigValueAsync("Email:Smtp:Host");
            var smtpUsername = await configService.GetConfigValueAsync("Email:Smtp:Username");
            var smtpPassword = await configService.GetConfigValueAsync("Email:Smtp:Password");

            // If Postmark is not configured, ensure SMTP is configured; otherwise we have no sender
            if (string.IsNullOrWhiteSpace(postmarkKey) && (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword)))
            {
                return Results.BadRequest(new 
                {
                    error = "SMTP not configured and Postmark not configured",
                    message = "Please configure Postmark (Email:Postmark:ApiKey) or SMTP (Email:Smtp:Host, Email:Smtp:Username, Email:Smtp:Password, Email:Smtp:Port) in admin settings",
                    configured = new
                    {
                        postmark = !string.IsNullOrWhiteSpace(postmarkKey),
                        host = !string.IsNullOrWhiteSpace(smtpHost),
                        username = !string.IsNullOrWhiteSpace(smtpUsername),
                        password = !string.IsNullOrWhiteSpace(smtpPassword)
                    }
                });
            }

            try
            {
                await emailService.SendPasswordResetEmailAsync(contact, "https://test-link.com/reset-test");
                return Results.Ok(new 
                {
                    success = true,
                    message = $"Test email sent successfully to {contact}",
                    usedPostmark = !string.IsNullOrWhiteSpace(postmarkKey)
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new 
                {
                    success = false,
                    error = "Failed to send test email",
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message
                }, statusCode: 500);
            }
        }

        // Treat as phone number (SMS placeholder)
        logger.LogInformation("Test SMS requested to {Phone} (SMS provider not implemented)", contact);
        return Results.Ok(new 
        { 
            success = true,
            message = $"Test SMS logged for {contact}"
        });
    }

    // Debug: Send a test email directly via Azure Communication Services and return the operation id
    private static async Task<IResult> SendTestEmailAzureAsync(
        [FromBody] TestEmailRequest request,
        IAppConfigService configService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AdminEndpoints");
        var to = !string.IsNullOrWhiteSpace(request.EmailOrPhone) ? request.EmailOrPhone : request.ToEmail;
        if (string.IsNullOrWhiteSpace(to) || !to.Contains("@"))
            return Results.BadRequest(new { error = "ToEmail is required" });

        var conn = await configService.GetConfigValueAsync("Azure:Communication:ConnectionString");
        var sender = await configService.GetConfigValueAsync("Azure:Communication:SenderAddress");

        if (string.IsNullOrWhiteSpace(conn) || string.IsNullOrWhiteSpace(sender))
            return Results.BadRequest(new { error = "Azure Communication Services not configured (ConnectionString or SenderAddress missing)" });

        try
        {
            // Validate the connection string access key before calling Azure so we return a clear error
            try
            {
                var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var accessPart = parts.FirstOrDefault(p => p.TrimStart().StartsWith("accesskey=", StringComparison.OrdinalIgnoreCase));
                if (accessPart == null)
                {
                    return Results.Problem(detail: "Azure Communication Services connection string missing accesskey", statusCode: 400, title: "Azure configuration error");
                }
                var accessValue = accessPart.Split('=', 2)[1];
                Convert.FromBase64String(accessValue);
            }
            catch (FormatException fe)
            {
                logger.LogError(fe, "Azure connection string accesskey is not valid Base64");
                return Results.Problem(detail: "Azure Communication Services connection string access key is invalid (not base64)", statusCode: 400, title: "Azure configuration error");
            }

            var client = new Azure.Communication.Email.EmailClient(conn);
            var content = new Azure.Communication.Email.EmailContent("Test Email from Ryve Rental via Azure")
            {
                PlainText = "This is a test email sent via Azure Communication Services.",
                Html = "<strong>This is a test email sent via Azure Communication Services.</strong>"
            };

            var recipients = new Azure.Communication.Email.EmailRecipients(new List<Azure.Communication.Email.EmailAddress> { new Azure.Communication.Email.EmailAddress(to) });
            var emailMessage = new Azure.Communication.Email.EmailMessage(senderAddress: sender, content: content, recipients: recipients);
            var op = await client.SendAsync(Azure.WaitUntil.Started, emailMessage);
            logger.LogInformation("Azure send started: MessageId={MessageId}", op.Id);

            return Results.Ok(new { success = true, message = "Azure send started", messageId = op.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send test email via Azure");
            return Results.Problem(detail: ex.Message, statusCode: 500, title: "Azure send failed");
        }
    }

    // Mileage charging settings handlers
    private static async Task<IResult> GetMileageSettingsAsync(AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (setting == null)
        {
            // Return defaults
            return Results.Ok(new MileageChargingSettings());
        }

        var settings = JsonSerializer.Deserialize<MileageChargingSettings>(setting.ValueJson);
        return Results.Ok(settings ?? new MileageChargingSettings());
    }

    private static async Task<IResult> UpdateMileageSettingsAsync(
        [FromBody] MileageChargingSettings request,
        AppDbContext db)
    {
        // Validation
        if (request.MinimumIncludedKilometers < 0)
            return Results.BadRequest(new { error = "Minimum included kilometers cannot be negative" });

        if (request.MinPricePerExtraKm < 0 || request.MaxPricePerExtraKm < 0)
            return Results.BadRequest(new { error = "Price per extra km cannot be negative" });

        if (request.MinPricePerExtraKm > request.MaxPricePerExtraKm)
            return Results.BadRequest(new { error = "Min price cannot be greater than max price" });

        if (request.DefaultPricePerExtraKm < request.MinPricePerExtraKm || 
            request.DefaultPricePerExtraKm > request.MaxPricePerExtraKm)
            return Results.BadRequest(new { error = "Default price must be within min/max range" });

        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "MileageCharging");
        if (setting == null)
        {
            setting = new GlobalSetting { Key = "MileageCharging" };
            db.GlobalSettings.Add(setting);
        }

        setting.ValueJson = JsonSerializer.Serialize(request);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Mileage charging settings updated successfully", settings = request });
    }

    private static async Task<IResult> GetNotificationSettingsAsync(AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "NotificationSettings");
        
        if (setting == null)
        {
            // Return default settings
            return Results.Ok(new NotificationSettings());
        }

        try
        {
            var settings = JsonSerializer.Deserialize<NotificationSettings>(setting.ValueJson);
            return Results.Ok(settings);
        }
        catch
        {
            return Results.Ok(new NotificationSettings());
        }
    }

    private static async Task<IResult> UpdateNotificationSettingsAsync(
        [FromBody] NotificationSettings request,
        AppDbContext db)
    {
        var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "NotificationSettings");
        if (setting == null)
        {
            setting = new GlobalSetting { Key = "NotificationSettings" };
            db.GlobalSettings.Add(setting);
        }

        setting.ValueJson = JsonSerializer.Serialize(request);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Notification settings updated successfully", settings = request });
    }

    private class AdminCreateNotificationJobRequest
    {
        public Guid? BookingId { get; set; }
        public Guid? TargetUserId { get; set; }
        public string? TargetEmail { get; set; }
        public string? TargetPhone { get; set; }
        public List<string>? Channels { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? TemplateName { get; set; }
        public object? Metadata { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public bool SendImmediately { get; set; } = true;
    }

    private static async Task<IResult> CreateNotificationJobAsync(
        [FromBody] AdminCreateNotificationJobRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        INotificationService notificationService)
    {
        // Validate
        if (request.Channels == null || !request.Channels.Any())
            return Results.BadRequest(new { error = "At least one channel is required" });

        var job = new NotificationJob
        {
            BookingId = request.BookingId,
            TargetUserId = request.TargetUserId,
            TargetEmail = request.TargetEmail,
            TargetPhone = request.TargetPhone,
            ChannelsJson = JsonSerializer.Serialize(request.Channels.Select(c => c.ToLowerInvariant())),
            Subject = request.Subject ?? string.Empty,
            Message = request.Message ?? string.Empty,
            TemplateName = request.TemplateName,
            MetadataJson = request.Metadata == null ? null : JsonSerializer.Serialize(request.Metadata),
            ScheduledAt = request.ScheduledAt,
            SendImmediately = request.SendImmediately,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var id = await notificationService.CreateNotificationJobAsync(job);
        return Results.Created($"/api/v1/admin/notifications/{id}", new { id });
    }

    private static async Task<IResult> ListNotificationJobsAsync(
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.NotificationJobs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var jobs = await query.OrderByDescending(j => j.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = jobs });
    }

    // Admin personal notifications
    private static async Task<IResult> GetAdminNotificationsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Notifications.Where(n => n.UserId == userId);
        
        if (unreadOnly == true)
            query = query.Where(n => !n.Read);

        var total = await query.CountAsync();
        var unreadCount = await db.Notifications.CountAsync(n => n.UserId == userId && !n.Read);
        
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Group by time periods
        var now = DateTime.UtcNow;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var thisWeek = today.AddDays(-(int)now.DayOfWeek);

        var grouped = new
        {
            today = items.Where(n => n.CreatedAt.Date == today).ToList(),
            yesterday = items.Where(n => n.CreatedAt.Date == yesterday).ToList(),
            thisWeek = items.Where(n => n.CreatedAt.Date >= thisWeek && n.CreatedAt.Date < yesterday).ToList(),
            older = items.Where(n => n.CreatedAt.Date < thisWeek).ToList()
        };

        return Results.Ok(new
        {
            total,
            unreadCount,
            page,
            pageSize,
            data = items,
            grouped
        });
    }

    private static async Task<IResult> MarkAdminNotificationReadAsync(
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

    private static async Task<IResult> DeleteAdminNotificationAsync(
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

    private static async Task<IResult> MarkAllAdminNotificationsReadAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var unreadNotifications = await db.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ToListAsync();

        foreach (var n in unreadNotifications)
        {
            n.Read = true;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, markedCount = unreadNotifications.Count });
    }

    private static async Task<IResult> GetAdminNotificationPreferencesAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Results.NotFound(new { error = "User not found" });

        // Parse existing preferences or return defaults
        var preferences = new
        {
            emailNotifications = true,
            smsNotifications = false,
            newBooking = true,
            newVehicle = true,
            newReport = true,
            payoutRequest = true,
            verificationRequest = true,
            systemAlerts = true
        };

        if (!string.IsNullOrWhiteSpace(user.NotificationPreferencesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, bool>>(user.NotificationPreferencesJson);
                if (parsed != null)
                {
                    return Results.Ok(parsed);
                }
            }
            catch { }
        }

        return Results.Ok(preferences);
    }

    private static async Task<IResult> UpdateAdminNotificationPreferencesAsync(
        [FromBody] Dictionary<string, bool> preferences,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return Results.NotFound(new { error = "User not found" });

        user.NotificationPreferencesJson = JsonSerializer.Serialize(preferences);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, preferences });
    }

    // Maintenance helper: add NotificationPreferencesJson columns to Users and OwnerProfiles
    private static async Task<IResult> AdminAddNotificationColumnsAsync(
        HttpRequest request,
        AppDbContext db)
    {
        if (!request.Headers.TryGetValue("X-Confirm-Action", out var val) || val != "add-notification-columns")
            return Results.BadRequest(new { error = "Missing or invalid X-Confirm-Action header. Set header: X-Confirm-Action: add-notification-columns" });

        var sql = @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""NotificationPreferencesJson"" text NULL;
ALTER TABLE ""OwnerProfiles"" ADD COLUMN IF NOT EXISTS ""NotificationPreferencesJson"" text NULL;";

        try
        {
            await db.Database.ExecuteSqlRawAsync(sql);
            return Results.Ok(new { success = true, message = "Columns ensured (NotificationPreferencesJson added if missing)" });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
}

