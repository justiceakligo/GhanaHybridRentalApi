using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using GhanaHybridRentalApi.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class PartnerEndpoints
{
    public static void MapPartnerEndpoints(this WebApplication app)
    {
        // Admin endpoints - Partner management
        var adminGroup = app.MapGroup("/api/v1/admin/partners")
            .RequireAuthorization("AdminOnly");

        adminGroup.MapPost("", CreatePartnerAsync);
        adminGroup.MapPut("{partnerId:guid}", UpdatePartnerAsync);
        adminGroup.MapPost("{partnerId:guid}/images", UploadPartnerImagesAsync);
        adminGroup.MapGet("", ListPartnersAsync);
        adminGroup.MapGet("{partnerId:guid}", GetPartnerAsync);
        adminGroup.MapPost("{partnerId:guid}/deactivate", DeactivatePartnerAsync);
        adminGroup.MapPost("{partnerId:guid}/activate", ActivatePartnerAsync);
        adminGroup.MapDelete("{partnerId:guid}", DeletePartnerAsync);
        adminGroup.MapPost("{partnerId:guid}/verify", VerifyPartnerAsync);

        // Public endpoints - Partner suggestions and public listing
        app.MapGet("/api/v1/partners/suggestions", GetPartnerSuggestionsAsync);
        app.MapGet("/api/v1/partners", GetPublicPartnersAsync);
        
        // Authenticated endpoints
        var authGroup = app.MapGroup("/api/v1")
            .RequireAuthorization();

        authGroup.MapGet("/bookings/{bookingId:guid}/partner-suggestions", GetBookingPartnerSuggestionsAsync);
        authGroup.MapGet("/owner/partner-suggestions", GetOwnerPartnerSuggestionsAsync);
        authGroup.MapGet("/owner/vehicles/{vehicleId:guid}/partner-suggestions", GetVehiclePartnerSuggestionsAsync);
        
        // Tracking endpoints
        authGroup.MapPost("/partners/{partnerId:guid}/click", TrackPartnerClickAsync);
        authGroup.MapPost("/partners/{partnerId:guid}/conversion", TrackPartnerConversionAsync);

        // Analytics endpoints
        adminGroup.MapGet("/analytics", GetPartnerAnalyticsAsync);
        adminGroup.MapGet("{partnerId:guid}/analytics", GetPartnerDetailAnalyticsAsync);
    }

    // ==================== ADMIN ENDPOINTS ====================

    private static async Task<IResult> CreatePartnerAsync(
        CreatePartnerRequest request,
        AppDbContext db,
        ILoggerFactory loggerFactory,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        var logger = loggerFactory.CreateLogger("PartnerEndpoints");
        var exposeDetailedErrors = config.GetValue<bool>("Diagnostics:ExposeDetailedErrors", false);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Country))
            return Results.BadRequest(new { error = "Name, city, and country are required" });

        if (request.TargetRoles is null || request.TargetRoles.Count == 0)
            return Results.BadRequest(new { error = "At least one targetRole is required" });

        if (request.Categories is null || request.Categories.Count == 0)
            return Results.BadRequest(new { error = "At least one category is required" });

        // Enforce max lengths to avoid DB truncation errors (create local sanitized variables)
        var name = request.Name.Length > 128 ? request.Name[..128] : request.Name;
        var description = !string.IsNullOrWhiteSpace(request.Description) ? (request.Description.Length > 1000 ? request.Description[..1000] : request.Description) : string.Empty;
        var city = !string.IsNullOrWhiteSpace(request.City) ? (request.City.Length > 64 ? request.City[..64] : request.City) : string.Empty;
        var country = !string.IsNullOrWhiteSpace(request.Country) ? (request.Country.Length > 8 ? request.Country[..8] : request.Country) : string.Empty;
        var referralCode = !string.IsNullOrWhiteSpace(request.ReferralCode) ? (request.ReferralCode.Length > 64 ? request.ReferralCode[..64] : request.ReferralCode) : null;
            var partner = new Partner
        {
            Name = name,
            Description = description,
            LogoUrl = request.LogoUrl,
            WebsiteUrl = request.WebsiteUrl,
            PhoneNumber = request.PhoneNumber,
            City = city,
            Country = country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TargetRoles = string.Join(",", request.TargetRoles),
            Categories = string.Join(",", request.Categories),
            PriorityScore = request.PriorityScore,
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            ReferralCode = referralCode,
            Metadata = request.Metadata,
            ImageUrlsJson = request.ImageUrls is not null && request.ImageUrls.Count > 0 ? JsonSerializer.Serialize(request.ImageUrls) : null,
            TagsJson = request.Tags is not null && request.Tags.Count > 0 ? JsonSerializer.Serialize(request.Tags) : null,
            ContactJson = request.Contact is not null ? JsonSerializer.Serialize(request.Contact) : null,
            BusinessHoursJson = request.BusinessHours is not null && request.BusinessHours.Count > 0 ? JsonSerializer.Serialize(request.BusinessHours) : null,
            IsVerified = request.IsVerified ?? false,
            VerificationBadge = request.VerificationBadge,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        db.Partners.Add(partner);
        try
        {
            await db.SaveChangesAsync();
            // If initial ImageUrls were provided, persist them in PartnerPhotos
            if (request.ImageUrls is not null && request.ImageUrls.Count > 0)
            {
                var photos = new List<PartnerPhoto>();
                var order = 0;
                foreach (var url in request.ImageUrls)
                {
                    photos.Add(new PartnerPhoto
                    {
                        PartnerId = partner.Id,
                        Url = url,
                        AltText = null,
                        DisplayOrder = order++,
                        IsLogo = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await db.PartnerPhotos.AddRangeAsync(photos);
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating partner");
            if (env.IsDevelopment() || exposeDetailedErrors)
                return Results.Problem(ex.ToString());
            return Results.Problem("An error occurred while saving changes.");
        }

        var createdResponse = MapToAdminPartnerResponse(partner);
        return Results.Created($"/api/v1/admin/partners/{partner.Id}", createdResponse);
    }

    private static async Task<IResult> UpdatePartnerAsync(
        Guid partnerId,
        UpdatePartnerRequest request,
        AppDbContext db)
    {
        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        if (request.Name is not null) partner.Name = request.Name;
        if (request.Description is not null) partner.Description = request.Description;
        if (request.LogoUrl is not null) partner.LogoUrl = request.LogoUrl;
        if (request.WebsiteUrl is not null) partner.WebsiteUrl = request.WebsiteUrl;
        if (request.PhoneNumber is not null) partner.PhoneNumber = request.PhoneNumber;
        if (request.City is not null) partner.City = request.City;
        if (request.Country is not null) partner.Country = request.Country;
        if (request.Latitude.HasValue) partner.Latitude = request.Latitude;
        if (request.Longitude.HasValue) partner.Longitude = request.Longitude;
        if (request.TargetRoles is not null) partner.TargetRoles = string.Join(",", request.TargetRoles);
        if (request.Categories is not null) partner.Categories = string.Join(",", request.Categories);
        if (request.PriorityScore.HasValue) partner.PriorityScore = request.PriorityScore.Value;
        if (request.IsFeatured.HasValue) partner.IsFeatured = request.IsFeatured.Value;
        if (request.IsActive.HasValue) partner.IsActive = request.IsActive.Value;
        if (request.ReferralCode is not null) partner.ReferralCode = request.ReferralCode;
        if (request.Metadata is not null) partner.Metadata = request.Metadata;
            // Keep ImageUrlsJson for backward compatibility (append URLs to JSON field)
            var existing = new List<string>();
            if (!string.IsNullOrWhiteSpace(partner.ImageUrlsJson))
            {
                try { existing = JsonSerializer.Deserialize<List<string>>(partner.ImageUrlsJson) ?? new List<string>(); } catch { existing = new List<string>(); }
            }
            existing.AddRange(request.ImageUrls ?? new List<string>());
            partner.ImageUrlsJson = JsonSerializer.Serialize(existing);
        if (request.Tags is not null) partner.TagsJson = JsonSerializer.Serialize(request.Tags);
        if (request.Contact is not null) partner.ContactJson = JsonSerializer.Serialize(request.Contact);
        if (request.BusinessHours is not null) partner.BusinessHoursJson = JsonSerializer.Serialize(request.BusinessHours);
        if (request.IsVerified.HasValue) partner.IsVerified = request.IsVerified.Value;
        if (request.VerificationBadge is not null) partner.VerificationBadge = request.VerificationBadge;

        partner.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var contact = string.IsNullOrWhiteSpace(partner.ContactJson) ? null : JsonSerializer.Deserialize<ContactInfo>(partner.ContactJson);
        var images = await db.PartnerPhotos.Where(pp => pp.PartnerId == partner.Id).OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToListAsync();

        var respDto = MapToAdminPartnerResponse(partner);
        return Results.Ok(respDto);
    }

    private static async Task<IResult> ListPartnersAsync(
        AppDbContext db,
        [FromQuery] string? city,
        [FromQuery] string? role,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isFeatured,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Partners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower() == city.ToLower());

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(p => p.TargetRoles.Contains(role.ToLower()));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Categories.Contains(category.ToLower()));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        var total = await query.CountAsync();
        var partners = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Photos)
            .ToListAsync();

        var result = partners.Select(p => MapToAdminPartnerResponse(p)).ToList();

        return Results.Ok(new
        {
            success = true,
            total,
            page,
            pageSize,
            data = result
        });
    }

    private static async Task<IResult> GetPartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.Partners
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        var images = partner.Photos != null ? partner.Photos.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToList() : null;
        var tags = string.IsNullOrWhiteSpace(partner.TagsJson) ? null : JsonSerializer.Deserialize<List<string>>(partner.TagsJson);
        var contact = string.IsNullOrWhiteSpace(partner.ContactJson) ? null : JsonSerializer.Deserialize<ContactInfo>(partner.ContactJson);

        var response = MapToAdminPartnerResponse(partner);

        return Results.Ok(response);
    }

    private static async Task<IResult> UploadPartnerImagesAsync(
        Guid partnerId,
        HttpRequest request,
        IFileUploadService fileUploadService,
        AppDbContext db)
    {
        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "No form content" });

        var form = await request.ReadFormAsync();
        var files = form.Files;
        if (files is null || files.Count == 0)
            return Results.BadRequest(new { error = "No files provided" });

        // Limit to 10 files
        if (files.Count > 10)
            return Results.BadRequest(new { error = "Maximum 10 images allowed" });

        var uploadList = new List<(Stream stream, string fileName, string contentType)>();
        foreach (var f in files)
        {
            if (f.Length <= 0) continue;
            if (f.Length > 10 * 1024 * 1024) // 10MB limit
                return Results.BadRequest(new { error = "File too large (max 10MB)" });

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(f.ContentType))
                return Results.BadRequest(new { error = $"Unsupported content type: {f.ContentType}" });

            uploadList.Add((f.OpenReadStream(), f.FileName, f.ContentType));
        }

        var uploadedUrls = await fileUploadService.UploadMultipleFilesAsync(uploadList);

        // Save uploaded files as PartnerPhoto rows and link to partner
        var maxOrder = partner.Photos?.Max(p => p.DisplayOrder) ?? -1;
        var newPhotos = new List<PartnerPhoto>();
        foreach (var url in uploadedUrls)
        {
            maxOrder++;
            var photo = new PartnerPhoto
            {
                PartnerId = partner.Id,
                Url = url,
                AltText = null,
                DisplayOrder = maxOrder,
                IsLogo = false,
                CreatedAt = DateTime.UtcNow
            };
            newPhotos.Add(photo);
        }
        await db.PartnerPhotos.AddRangeAsync(newPhotos);
        partner.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, urls = uploadedUrls });
    }

    private static async Task<IResult> DeactivatePartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.Partners.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        partner.IsActive = false;
        partner.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var response = MapToAdminPartnerResponse(partner);
        return Results.Ok(response);
    }

    private static async Task<IResult> ActivatePartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.Partners.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        partner.IsActive = true;
        partner.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var response = MapToAdminPartnerResponse(partner);
        return Results.Ok(response);
    }

    private static async Task<IResult> DeletePartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        db.Partners.Remove(partner);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Partner deleted successfully" });
    }

    private static async Task<IResult> VerifyPartnerAsync(
        Guid partnerId,
        VerifyPartnerRequest request,
        AppDbContext db)
    {
        var partner = await db.Partners.Include(p => p.Photos).FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        partner.IsVerified = request.IsVerified;
        partner.VerificationBadge = request.VerificationBadge;
        partner.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        var response = MapToAdminPartnerResponse(partner);
        return Results.Ok(response);
    }

    public record VerifyPartnerRequest(bool IsVerified, string? VerificationBadge);

    // ==================== PUBLIC/RENTER ENDPOINTS ====================

    private static async Task<IResult> GetPartnerSuggestionsAsync(
        AppDbContext db,
        [FromQuery] string? city,
        [FromQuery] string? role,
        [FromQuery] string? category,
        [FromQuery] int limit = 5)
    {
        var query = db.Partners
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower() == city.ToLower());

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(p => p.TargetRoles.Contains(role.ToLower()));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Categories.Contains(category.ToLower()));

        var partnerEntities = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .ThenBy(p => Guid.NewGuid()) // Randomize within same priority
            .Take(limit)
            .Include(p => p.Photos)
            .ToListAsync();

        var partners = partnerEntities.Select(p => new PartnerSuggestionResponse(
            p.Id,
            p.Name,
            p.Description,
            p.LogoUrl,
            p.WebsiteUrl,
            p.PhoneNumber,
            p.City,
            p.Country,
            p.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.TargetRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.PriorityScore,
            p.IsFeatured,
            p.ReferralCode,
            p.Photos != null ? p.Photos.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToList() : (List<string>?)null,
            string.IsNullOrWhiteSpace(p.TagsJson) ? null : JsonSerializer.Deserialize<List<string>>(p.TagsJson)
        )).ToList();

        return Results.Ok(new
        {
            success = true,
            city,
            role,
            category,
            count = partners.Count,
            partners
        });
    }

    private static async Task<IResult> GetPublicPartnersAsync(
        AppDbContext db,
        [FromQuery] string? city,
        [FromQuery] string? role,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Partners.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower() == city.ToLower());

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(p => p.TargetRoles.Contains(role.ToLower()));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Categories.Contains(category.ToLower()));

        var total = await query.CountAsync();
        var partnerEntities = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .ThenBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Photos)
            .ToListAsync();

        var partners = partnerEntities.Select(p => new PartnerSuggestionResponse(
            p.Id,
            p.Name,
            p.Description,
            p.LogoUrl,
            p.WebsiteUrl,
            p.PhoneNumber,
            p.City,
            p.Country,
            p.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.TargetRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.PriorityScore,
            p.IsFeatured,
            p.ReferralCode,
            p.Photos != null ? p.Photos.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToList() : null,
            string.IsNullOrWhiteSpace(p.TagsJson) ? null : JsonSerializer.Deserialize<List<string>>(p.TagsJson)
        )).ToList();

        return Results.Ok(new
        {
            success = true,
            city,
            role,
            category,
            total,
            page,
            pageSize,
            data = partners
        });
    }

    private static async Task<IResult> GetBookingPartnerSuggestionsAsync(
        Guid bookingId,
        AppDbContext db,
        HttpContext context)
    {
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.City)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var userIdClaim = context.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || Guid.Parse(userIdClaim) != booking.RenterId)
            return Results.Forbid();

        var city = booking.Vehicle?.City?.Name ?? "";
        if (string.IsNullOrWhiteSpace(city))
            return Results.Ok(new { success = true, message = "No city information available", partners = new { } });

        // Get partners by category for renters
        var allPartners = await db.Partners
            .Where(p => p.IsActive && 
                        p.City.ToLower() == city.ToLower() && 
                        p.TargetRoles.Contains("renter"))
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .ToListAsync();

        var groupedPartners = new
        {
            hotels = allPartners
                .Where(p => p.Categories.Contains("hotel"))
                .Take(3)
                .Select(p => ToSuggestionResponse(p))
                .ToList(),
            restaurants = allPartners
                .Where(p => p.Categories.Contains("restaurant"))
                .Take(3)
                .Select(p => ToSuggestionResponse(p))
                .ToList(),
            tours = allPartners
                .Where(p => p.Categories.Contains("tour"))
                .Take(3)
                .Select(p => ToSuggestionResponse(p))
                .ToList(),
            airportTransfers = allPartners
                .Where(p => p.Categories.Contains("airport_transfer"))
                .Take(2)
                .Select(p => ToSuggestionResponse(p))
                .ToList()
        };

        return Results.Ok(new
        {
            success = true,
            city,
            bookingId,
            partners = groupedPartners
        });
    }

    // ==================== OWNER ENDPOINTS ====================

    private static async Task<IResult> GetOwnerPartnerSuggestionsAsync(
        AppDbContext db,
        HttpContext context,
        [FromQuery] string? city,
        [FromQuery] int limit = 5)
    {
        var userIdClaim = context.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var user = await db.Users.FindAsync(userId);
        if (user is null || !string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        // If no city provided, get owner's first vehicle city
        if (string.IsNullOrWhiteSpace(city))
        {
            var vehicle = await db.Vehicles
                .Include(v => v.City)
                .FirstOrDefaultAsync(v => v.OwnerId == userId);
            city = vehicle?.City?.Name;
        }

        if (string.IsNullOrWhiteSpace(city))
            return Results.Ok(new { success = true, message = "No city information available", partners = new { } });

        var allPartners = await db.Partners
            .Where(p => p.IsActive && 
                        p.City.ToLower() == city!.ToLower() && 
                        p.TargetRoles.Contains("owner"))
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .ToListAsync();

        var groupedPartners = new
        {
            protection = allPartners
                .Where(p => p.Categories.Contains("gps_tracking") || p.Categories.Contains("insurance"))
                .Take(3)
                .Select(p => ToSuggestionResponse(p))
                .ToList(),
            maintenance = allPartners
                .Where(p => p.Categories.Contains("mechanic") || p.Categories.Contains("car_wash") || p.Categories.Contains("tyre_shop"))
                .Take(3)
                .Select(p => ToSuggestionResponse(p))
                .ToList()
        };

        return Results.Ok(new
        {
            success = true,
            city,
            partners = groupedPartners
        });
    }

    private static async Task<IResult> GetVehiclePartnerSuggestionsAsync(
        Guid vehicleId,
        AppDbContext db,
        HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return Results.Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var vehicle = await db.Vehicles
            .Include(v => v.City)
            .FirstOrDefaultAsync(v => v.Id == vehicleId);

        if (vehicle is null)
            return Results.NotFound(new { error = "Vehicle not found" });

        if (vehicle.OwnerId != userId)
            return Results.Forbid();

        var city = vehicle.City?.Name ?? "";
        if (string.IsNullOrWhiteSpace(city))
            return Results.Ok(new { success = true, message = "No city information available", partners = new { } });

        var allPartners = await db.Partners
            .Where(p => p.IsActive && 
                        p.City.ToLower() == city.ToLower() && 
                        p.TargetRoles.Contains("owner"))
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PriorityScore)
            .Take(5)
            .Select(p => ToSuggestionResponse(p))
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            vehicleId,
            city,
            partners = allPartners
        });
    }

    // ==================== TRACKING ENDPOINTS ====================

    private static async Task<IResult> TrackPartnerClickAsync(
        Guid partnerId,
        PartnerClickRequest request,
        AppDbContext db,
        HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("userId")?.Value;
        Guid? userId = string.IsNullOrEmpty(userIdClaim) ? null : Guid.Parse(userIdClaim);

        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        var click = new PartnerClick
        {
            PartnerId = partnerId,
            UserId = userId,
            BookingId = request.BookingId,
            Role = request.Role,
            City = request.City,
            EventType = "click",
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        db.PartnerClicks.Add(click);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Click tracked successfully" });
    }

    private static async Task<IResult> TrackPartnerConversionAsync(
        Guid partnerId,
        PartnerConversionRequest request,
        AppDbContext db,
        HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("userId")?.Value;
        Guid? userId = string.IsNullOrEmpty(userIdClaim) ? null : Guid.Parse(userIdClaim);

        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        var conversion = new PartnerClick
        {
            PartnerId = partnerId,
            UserId = userId,
            BookingId = request.BookingId,
            EventType = "conversion",
            ConversionAmount = request.Amount,
            ExternalReference = request.ExternalReference,
            CreatedAt = DateTime.UtcNow
        };

        db.PartnerClicks.Add(conversion);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Conversion tracked successfully" });
    }

    // ==================== ANALYTICS ENDPOINTS ====================

    private static async Task<IResult> GetPartnerAnalyticsAsync(
        AppDbContext db,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var clicks = await db.PartnerClicks
            .Include(c => c.Partner)
            .Where(c => c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
            .GroupBy(c => c.PartnerId)
            .Select(g => new
            {
                partnerId = g.Key,
                partnerName = g.First().Partner!.Name,
                clicks = g.Count(c => c.EventType == "click"),
                conversions = g.Count(c => c.EventType == "conversion"),
                totalConversionValue = g.Where(c => c.EventType == "conversion").Sum(c => c.ConversionAmount ?? 0)
            })
            .OrderByDescending(x => x.clicks)
            .ToListAsync();

        return Results.Ok(new
        {
            success = true,
            period = new { from = fromDate, to = toDate },
            analytics = clicks
        });
    }

    private static async Task<IResult> GetPartnerDetailAnalyticsAsync(
        Guid partnerId,
        AppDbContext db,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var partner = await db.Partners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var clicks = await db.PartnerClicks
            .Where(c => c.PartnerId == partnerId && c.CreatedAt >= fromDate && c.CreatedAt <= toDate)
            .ToListAsync();

        var clicksByDay = clicks
            .Where(c => c.EventType == "click")
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        var conversionsByDay = clicks
            .Where(c => c.EventType == "conversion")
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count(), value = g.Sum(x => x.ConversionAmount ?? 0) })
            .OrderBy(x => x.date)
            .ToList();

        return Results.Ok(new
        {
            success = true,
            partner,
            period = new { from = fromDate, to = toDate },
            analytics = new
            {
                totalClicks = clicks.Count(c => c.EventType == "click"),
                totalConversions = clicks.Count(c => c.EventType == "conversion"),
                totalConversionValue = clicks.Where(c => c.EventType == "conversion").Sum(c => c.ConversionAmount ?? 0),
                clicksByDay,
                conversionsByDay
            }
        });
    }

    // ==================== HELPER METHODS ====================

    private static PartnerSuggestionResponse ToSuggestionResponse(Partner p)
    {
        return new PartnerSuggestionResponse(
            p.Id,
            p.Name,
            p.Description,
            p.LogoUrl,
            p.WebsiteUrl,
            p.PhoneNumber,
            p.City,
            p.Country,
            p.Categories.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.TargetRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            p.PriorityScore,
            p.IsFeatured,
            p.ReferralCode,
            p.Photos != null ? p.Photos.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToList() : (List<string>?)null,
            string.IsNullOrWhiteSpace(p.TagsJson) ? null : JsonSerializer.Deserialize<List<string>>(p.TagsJson)
        );
    }

    private static AdminPartnerResponse MapToAdminPartnerResponse(Partner partner)
    {
        var images = partner.Photos != null ? partner.Photos.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.Url).ToList() : null;
        var tags = string.IsNullOrWhiteSpace(partner.TagsJson) ? null : JsonSerializer.Deserialize<List<string>>(partner.TagsJson);
        var contact = string.IsNullOrWhiteSpace(partner.ContactJson) ? null : JsonSerializer.Deserialize<ContactInfo>(partner.ContactJson);
        var targetRoles = partner.TargetRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        var categories = partner.Categories?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        return new AdminPartnerResponse(
            partner.Id,
            partner.Name,
            partner.Description,
            partner.LogoUrl,
            partner.WebsiteUrl,
            partner.PhoneNumber,
            images,
            tags,
            contact,
            partner.City,
            partner.Country,
            targetRoles,
            categories,
            partner.PriorityScore,
            partner.IsFeatured,
            partner.IsActive,
            partner.ReferralCode,
            partner.Metadata,
            partner.IsVerified,
            partner.VerificationBadge,
            partner.RatingAvg,
            partner.RatingCount,
            partner.CreatedAt,
            partner.UpdatedAt
        );
    }
}
