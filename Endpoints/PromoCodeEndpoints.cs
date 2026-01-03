using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GhanaHybridRentalApi.Endpoints;

public static class PromoCodeEndpoints
{
    public static void MapPromoCodeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1").RequireAuthorization();

        // ADMIN ENDPOINTS
        var adminGroup = group.MapGroup("/admin/promo-codes").RequireAuthorization(p => p.RequireRole("admin"));
        
        adminGroup.MapPost("", CreatePromoCodeAsync);
        adminGroup.MapGet("", GetAllPromoCodesAsync);
        adminGroup.MapGet("{id:guid}", GetPromoCodeByIdAsync);
        adminGroup.MapPut("{id:guid}", UpdatePromoCodeAsync);
        adminGroup.MapDelete("{id:guid}", DeletePromoCodeAsync);
        adminGroup.MapGet("{id:guid}/usage", GetPromoCodeUsageAsync);
        adminGroup.MapGet("{id:guid}/analytics", GetPromoCodeAnalyticsAsync);

        // RENTER/OWNER ENDPOINTS - Allow anonymous for guest checkout
        group.MapPost("/promo-codes/validate", ValidatePromoCodeAsync).AllowAnonymous();
        
        // OWNER REFERRAL ENDPOINTS
        var ownerGroup = group.MapGroup("/owner/referrals").RequireAuthorization(p => p.RequireRole("owner"));
        
        ownerGroup.MapPost("/generate", GenerateReferralCodeAsync);
        ownerGroup.MapGet("/stats", GetReferralStatsAsync);
        ownerGroup.MapGet("/codes", GetMyReferralCodesAsync);

        // OWNER VEHICLE PROMO ENDPOINTS
        var ownerPromoGroup = group.MapGroup("/owner/promo-codes").RequireAuthorization(p => p.RequireRole("owner"));
        
        ownerPromoGroup.MapGet("", GetMyVehiclePromoCodesAsync);
        ownerPromoGroup.MapPost("/vehicle", CreateOwnerVehiclePromoAsync);
        ownerPromoGroup.MapGet("/vehicle/{vehicleId:guid}", GetVehiclePromosAsync);
        ownerPromoGroup.MapDelete("/{id:guid}", DeactivateOwnerPromoAsync);
    }

    // ADMIN: Create promo code
    private static async Task<IResult> CreatePromoCodeAsync(
        CreatePromoCodeDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Check if code already exists
        if (await db.PromoCodes.AnyAsync(p => p.Code.ToUpper() == dto.Code.ToUpper()))
            return Results.BadRequest(new { error = "Promo code already exists" });

        var promoCode = new PromoCode
        {
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            PromoType = dto.PromoType,
            DiscountValue = dto.DiscountValue,
            TargetUserType = dto.TargetUserType,
            AppliesTo = dto.AppliesTo,
            MinimumBookingAmount = dto.MinimumBookingAmount,
            MaximumDiscountAmount = dto.MaximumDiscountAmount,
            ValidFrom = dto.ValidFrom,
            ValidUntil = dto.ValidUntil,
            MaxTotalUses = dto.MaxTotalUses,
            MaxUsesPerUser = dto.MaxUsesPerUser,
            FirstTimeUsersOnly = dto.FirstTimeUsersOnly,
            VehicleId = dto.VehicleId,
            CategoryId = dto.CategoryId,
            CityId = dto.CityId,
            CreatedBy = "admin",
            CreatedByUserId = userId
        };

        db.PromoCodes.Add(promoCode);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/promo-codes/{promoCode.Id}", promoCode);
    }

    // ADMIN: Get all promo codes
    private static async Task<IResult> GetAllPromoCodesAsync(
        AppDbContext db,
        [FromQuery] bool? isActive,
        [FromQuery] string? targetUserType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.PromoCodes
            .Include(p => p.Category)
            .Include(p => p.City)
            .Include(p => p.Referrer)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(targetUserType))
        {
            if (Enum.TryParse<TargetUserType>(targetUserType, true, out var userType))
                query = query.Where(p => p.TargetUserType == userType || p.TargetUserType == TargetUserType.Both);
        }

        var total = await query.CountAsync();
        var promoCodes = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PromoCodeDetailsDto(
                p.Id,
                p.Code,
                p.Description,
                p.PromoType.ToString(),
                p.DiscountValue,
                p.TargetUserType.ToString(),
                p.AppliesTo.ToString(),
                p.MinimumBookingAmount,
                p.MaximumDiscountAmount,
                p.ValidFrom,
                p.ValidUntil,
                p.MaxTotalUses,
                p.MaxUsesPerUser,
                p.CurrentTotalUses,
                p.IsActive,
                p.FirstTimeUsersOnly,
                p.IsReferralCode,
                p.Referrer != null ? $"{p.Referrer.FirstName} {p.Referrer.LastName}" : null,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.CityId,
                p.City != null ? p.City.Name : null,
                p.CreatedAt
            ))
            .ToListAsync();

        return Results.Ok(new { promoCodes, total, page, pageSize });
    }

    // ADMIN: Get promo code by ID
    private static async Task<IResult> GetPromoCodeByIdAsync(
        Guid id,
        AppDbContext db)
    {
        var promoCode = await db.PromoCodes
            .Include(p => p.Category)
            .Include(p => p.City)
            .Include(p => p.Referrer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (promoCode == null)
            return Results.NotFound(new { error = "Promo code not found" });

        var dto = new PromoCodeDetailsDto(
            promoCode.Id,
            promoCode.Code,
            promoCode.Description,
            promoCode.PromoType.ToString(),
            promoCode.DiscountValue,
            promoCode.TargetUserType.ToString(),
            promoCode.AppliesTo.ToString(),
            promoCode.MinimumBookingAmount,
            promoCode.MaximumDiscountAmount,
            promoCode.ValidFrom,
            promoCode.ValidUntil,
            promoCode.MaxTotalUses,
            promoCode.MaxUsesPerUser,
            promoCode.CurrentTotalUses,
            promoCode.IsActive,
            promoCode.FirstTimeUsersOnly,
            promoCode.IsReferralCode,
            promoCode.Referrer != null ? $"{promoCode.Referrer.FirstName} {promoCode.Referrer.LastName}" : null,
            promoCode.CategoryId,
            promoCode.Category?.Name,
            promoCode.CityId,
            promoCode.City?.Name,
            promoCode.CreatedAt
        );

        return Results.Ok(dto);
    }

    // ADMIN: Update promo code
    private static async Task<IResult> UpdatePromoCodeAsync(
        Guid id,
        UpdatePromoCodeDto dto,
        AppDbContext db)
    {
        var promoCode = await db.PromoCodes.FindAsync(id);
        if (promoCode == null)
            return Results.NotFound(new { error = "Promo code not found" });

        if (dto.Description != null) promoCode.Description = dto.Description;
        if (dto.DiscountValue.HasValue) promoCode.DiscountValue = dto.DiscountValue.Value;
        if (dto.MinimumBookingAmount.HasValue) promoCode.MinimumBookingAmount = dto.MinimumBookingAmount;
        if (dto.MaximumDiscountAmount.HasValue) promoCode.MaximumDiscountAmount = dto.MaximumDiscountAmount;
        if (dto.ValidFrom.HasValue) promoCode.ValidFrom = dto.ValidFrom.Value;
        if (dto.ValidUntil.HasValue) promoCode.ValidUntil = dto.ValidUntil.Value;
        if (dto.MaxTotalUses.HasValue) promoCode.MaxTotalUses = dto.MaxTotalUses;
        if (dto.MaxUsesPerUser.HasValue) promoCode.MaxUsesPerUser = dto.MaxUsesPerUser.Value;
        if (dto.IsActive.HasValue) promoCode.IsActive = dto.IsActive.Value;

        promoCode.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(promoCode);
    }

    // ADMIN: Delete promo code
    private static async Task<IResult> DeletePromoCodeAsync(
        Guid id,
        AppDbContext db)
    {
        var promoCode = await db.PromoCodes.FindAsync(id);
        if (promoCode == null)
            return Results.NotFound(new { error = "Promo code not found" });

        // Soft delete by marking inactive
        promoCode.IsActive = false;
        promoCode.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Promo code deactivated successfully" });
    }

    // ADMIN: Get promo code usage history
    private static async Task<IResult> GetPromoCodeUsageAsync(
        Guid id,
        AppDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var total = await db.PromoCodeUsage.CountAsync(u => u.PromoCodeId == id);
        
        var usages = await db.PromoCodeUsage
            .Include(u => u.UsedByUser)
            .Include(u => u.Booking)
            .Include(u => u.Referrer)
            .Where(u => u.PromoCodeId == id)
            .OrderByDescending(u => u.UsedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new PromoCodeUsageDto(
                u.Id,
                u.Code,
                u.UsedByUser.Email,
                u.UserType,
                u.BookingId,
                u.Booking != null ? u.Booking.BookingReference : null,
                u.OriginalAmount,
                u.DiscountAmount,
                u.FinalAmount,
                u.AppliedTo,
                u.ReferrerUserId,
                u.Referrer != null ? $"{u.Referrer.FirstName} {u.Referrer.LastName}" : null,
                u.ReferrerRewardAmount,
                u.ReferrerRewardApplied,
                u.UsedAt
            ))
            .ToListAsync();

        return Results.Ok(new { usages, total, page, pageSize });
    }

    // ADMIN: Get promo code analytics
    private static async Task<IResult> GetPromoCodeAnalyticsAsync(
        Guid id,
        AppDbContext db)
    {
        var promoCode = await db.PromoCodes.FindAsync(id);
        if (promoCode == null)
            return Results.NotFound(new { error = "Promo code not found" });

        var usages = await db.PromoCodeUsage
            .Where(u => u.PromoCodeId == id)
            .ToListAsync();

        var totalUses = usages.Count;
        var uniqueUsers = usages.Select(u => u.UsedByUserId).Distinct().Count();
        var totalDiscountGiven = usages.Sum(u => u.DiscountAmount);
        var averageDiscount = totalUses > 0 ? totalDiscountGiven / totalUses : 0;

        var newCustomers = await db.PromoCodeUsage
            .Where(u => u.PromoCodeId == id)
            .Select(u => u.UsedByUserId)
            .Distinct()
            .CountAsync(userId => !db.Bookings.Any(b => 
                (b.RenterId == userId || b.OwnerId == userId) && 
                b.PromoCodeId != id));

        var revenueGenerated = await db.Bookings
            .Where(b => b.PromoCodeId == id && b.Status != "cancelled")
            .SumAsync(b => b.TotalAmount);

        var usageByDate = usages
            .GroupBy(u => u.UsedAt.Date)
            .Select(g => new UsageByDateDto(
                g.Key,
                g.Count(),
                g.Sum(u => u.DiscountAmount)
            ))
            .OrderBy(u => u.Date)
            .ToList();

        var analytics = new PromoCodeAnalyticsDto(
            promoCode.Code,
            totalUses,
            uniqueUsers,
            totalDiscountGiven,
            averageDiscount,
            newCustomers,
            revenueGenerated,
            usageByDate
        );

        return Results.Ok(analytics);
    }

    // RENTER/OWNER: Validate promo code (supports both authenticated and guest users)
    private static async Task<IResult> ValidatePromoCodeAsync(
        ValidatePromoCodeDto dto,
        ClaimsPrincipal principal,
        IPromoCodeService promoCodeService)
    {
        // For guest users, use Guid.Empty (user-specific validations will be skipped in service)
        var userId = Guid.Empty;
        
        // If user is authenticated, get their userId for user-specific validations
        var userIdStr = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var result = await promoCodeService.ValidatePromoCodeAsync(dto.Code, userId, dto);
        return Results.Ok(result);
    }

    // OWNER: Generate referral code
    private static async Task<IResult> GenerateReferralCodeAsync(
        GenerateReferralCodeDto dto,
        ClaimsPrincipal principal,
        IPromoCodeService promoCodeService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        try
        {
            var code = await promoCodeService.GenerateReferralCodeAsync(userId, dto);
            return Results.Ok(new { code, message = "Referral code generated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // OWNER: Get referral stats
    private static async Task<IResult> GetReferralStatsAsync(
        ClaimsPrincipal principal,
        IPromoCodeService promoCodeService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var stats = await promoCodeService.GetUserReferralStatsAsync(userId);
        return Results.Ok(stats);
    }

    // OWNER: Get my referral codes
    private static async Task<IResult> GetMyReferralCodesAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var codes = await db.PromoCodes
            .Where(p => p.IsReferralCode && p.ReferrerUserId == userId)
            .Select(p => new ReferralCodeDto(
                p.Code,
                p.Referrer != null ? $"{p.Referrer.FirstName} {p.Referrer.LastName}" : "",
                p.ReferralRewardType ?? Models.ReferralRewardType.Credit,
                p.ReferralRewardValue ?? 0,
                p.CurrentTotalUses,
                0, // Calculate total rewards earned separately
                p.ValidFrom,
                p.ValidUntil,
                p.IsActive
            ))
            .ToListAsync();

        return Results.Ok(codes);
    }

    // OWNER: Create vehicle-specific promo code
    private static async Task<IResult> CreateOwnerVehiclePromoAsync(
        CreateOwnerVehiclePromoDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Verify vehicle ownership
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.OwnerId == userId);
        if (vehicle == null)
            return Results.NotFound(new { error = "Vehicle not found or you don't own this vehicle" });

        // Generate code or use custom
        string code;
        if (!string.IsNullOrWhiteSpace(dto.CustomCode))
        {
            code = dto.CustomCode.ToUpper();
            if (await db.PromoCodes.AnyAsync(p => p.Code == code))
                return Results.BadRequest(new { error = "This code is already in use" });
        }
        else
        {
            // Auto-generate: PLATE + random 3 digits
            var plateBase = vehicle.PlateNumber?.Replace("-", "").ToUpper() ?? "VEHICLE";
            var random = new Random();
            do
            {
                code = $"{plateBase}{random.Next(100, 999)}";
            } while (await db.PromoCodes.AnyAsync(p => p.Code == code));
        }

        var validFrom = dto.ValidFrom ?? DateTime.UtcNow;
        var validUntil = dto.ValidUntil ?? DateTime.UtcNow.AddMonths(3);

        var promoCode = new PromoCode
        {
            Code = code,
            Description = dto.Description ?? $"Special discount for {vehicle.Make} {vehicle.Model}",
            PromoType = PromoCodeType.OwnerVehicleDiscount,
            DiscountValue = dto.DiscountValue,
            TargetUserType = TargetUserType.Renter,
            AppliesTo = DiscountAppliesTo.RentalAmount,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            MaxTotalUses = dto.MaxTotalUses,
            MaxUsesPerUser = dto.MaxUsesPerUser,
            IsActive = true,
            CreatedBy = "owner",
            CreatedByUserId = userId,
            VehicleId = dto.VehicleId
        };

        db.PromoCodes.Add(promoCode);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/owner/promo-codes/{promoCode.Id}", new { code, promoCode });
    }

    // OWNER: Get all my vehicle promo codes
    private static async Task<IResult> GetMyVehiclePromoCodesAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var promos = await db.PromoCodes
            .Include(p => p.Vehicle)
            .Where(p => p.CreatedByUserId == userId && p.VehicleId != null)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                id = p.Id,
                code = p.Code,
                description = p.Description,
                vehicleId = p.VehicleId,
                vehicleName = p.Vehicle != null ? $"{p.Vehicle.Make} {p.Vehicle.Model} {p.Vehicle.Year}" : null,
                promoType = p.PromoType.ToString(),
                discountValue = p.DiscountValue,
                appliesTo = p.AppliesTo.ToString(),
                validFrom = p.ValidFrom,
                validUntil = p.ValidUntil,
                maxTotalUses = p.MaxTotalUses,
                maxUsesPerUser = p.MaxUsesPerUser,
                currentTotalUses = p.CurrentTotalUses,
                isActive = p.IsActive,
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Results.Ok(new { promoCodes = promos, total = promos.Count });
    }

    // OWNER: Get vehicle promo codes
    private static async Task<IResult> GetVehiclePromosAsync(
        Guid vehicleId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Verify ownership
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.OwnerId == userId);
        if (vehicle == null)
            return Results.NotFound(new { error = "Vehicle not found" });

        var promos = await db.PromoCodes
            .Where(p => p.VehicleId == vehicleId && p.CreatedByUserId == userId)
            .Select(p => new
            {
                id = p.Id,
                code = p.Code,
                description = p.Description,
                promoType = p.PromoType.ToString(),
                discountValue = p.DiscountValue,
                validFrom = p.ValidFrom,
                validUntil = p.ValidUntil,
                maxTotalUses = p.MaxTotalUses,
                maxUsesPerUser = p.MaxUsesPerUser,
                currentTotalUses = p.CurrentTotalUses,
                isActive = p.IsActive,
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Results.Ok(promos);
    }

    // OWNER: Deactivate owner promo code
    private static async Task<IResult> DeactivateOwnerPromoAsync(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var promo = await db.PromoCodes.FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId);
        if (promo == null)
            return Results.NotFound(new { error = "Promo code not found" });

        promo.IsActive = false;
        promo.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Promo code deactivated successfully" });
    }
}
