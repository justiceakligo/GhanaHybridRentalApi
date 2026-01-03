using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GhanaHybridRentalApi.Services;

public interface IPromoCodeService
{
    Task<PromoCodeValidationResult> ValidatePromoCodeAsync(string code, Guid userId, ValidatePromoCodeDto request);
    Task<PromoCodeUsage> ApplyPromoCodeAsync(string code, Guid userId, Guid? bookingId, decimal originalAmount, string userType);
    Task<string> GenerateReferralCodeAsync(Guid userId, GenerateReferralCodeDto request);
    Task<UserReferralStatsDto> GetUserReferralStatsAsync(Guid userId);
    Task ProcessReferralRewardAsync(Guid promoCodeUsageId);
}

public class PromoCodeService : IPromoCodeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PromoCodeService> _logger;

    public PromoCodeService(AppDbContext db, ILogger<PromoCodeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PromoCodeValidationResult> ValidatePromoCodeAsync(
        string code,
        Guid userId,
        ValidatePromoCodeDto request)
    {
        var promoCode = await _db.PromoCodes
            .Include(p => p.Category)
            .Include(p => p.City)
            .Include(p => p.Vehicle)
            .Include(p => p.Referrer)
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper());

        if (promoCode == null)
        {
            return new PromoCodeValidationResult(
                false, "Promo code not found", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check if active
        if (!promoCode.IsActive)
        {
            return new PromoCodeValidationResult(
                false, "This promo code is no longer active", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check validity dates
        var now = DateTime.UtcNow;
        if (now < promoCode.ValidFrom || now > promoCode.ValidUntil)
        {
            return new PromoCodeValidationResult(
                false, "This promo code has expired or is not yet valid", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check max total uses
        if (promoCode.MaxTotalUses.HasValue && promoCode.CurrentTotalUses >= promoCode.MaxTotalUses.Value)
        {
            return new PromoCodeValidationResult(
                false, "This promo code has reached its maximum usage limit", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // For guest users (userId == Guid.Empty), skip user-specific validations
        // but allow them to validate and see the discount
        bool isGuest = userId == Guid.Empty;
        
        if (!isGuest)
        {
            // Check user type (only for authenticated users)
            var user = await _db.Users
                .Include(u => u.OwnerProfile)
                .Include(u => u.RenterProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new PromoCodeValidationResult(
                    false, "User not found", null, request.BookingAmount, 0, request.BookingAmount, "");
            }

            string userType = user.Role.ToLower();
            if (promoCode.TargetUserType == TargetUserType.Renter && userType != "renter")
            {
                return new PromoCodeValidationResult(
                    false, "This promo code is only for renters", null, request.BookingAmount, 0, request.BookingAmount, "");
            }

            if (promoCode.TargetUserType == TargetUserType.Owner && userType != "owner")
            {
                return new PromoCodeValidationResult(
                    false, "This promo code is only for owners", null, request.BookingAmount, 0, request.BookingAmount, "");
            }
        }
        else
        {
            // For guest users, only allow renter-targeted promo codes
            if (promoCode.TargetUserType == TargetUserType.Owner)
            {
                return new PromoCodeValidationResult(
                    false, "This promo code is only for owners. Please sign in to use it.", null, request.BookingAmount, 0, request.BookingAmount, "");
            }
        }

        // Check vehicle restriction (for owner vehicle discounts)
        if (promoCode.VehicleId.HasValue && request.VehicleId.HasValue && request.VehicleId != promoCode.VehicleId)
        {
            return new PromoCodeValidationResult(
                false, $"This promo code only applies to a specific vehicle", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check if first-time users only (skip for guest users)
        if (!isGuest && promoCode.FirstTimeUsersOnly)
        {
            var hasBookings = await _db.Bookings.AnyAsync(b => 
                (b.RenterId == userId || b.OwnerId == userId) && 
                b.Status != "cancelled");

            if (hasBookings)
            {
                return new PromoCodeValidationResult(
                    false, "This promo code is only for first-time users", null, request.BookingAmount, 0, request.BookingAmount, "");
            }
        }
        else if (isGuest && promoCode.FirstTimeUsersOnly)
        {
            // Guest can see the discount, but note that it's for first-time users
            // The actual validation will happen during booking creation when they sign up
        }

        // Check category restriction
        if (promoCode.CategoryId.HasValue && request.CategoryId != promoCode.CategoryId)
        {
            return new PromoCodeValidationResult(
                false, $"This promo code only applies to {promoCode.Category?.Name ?? "specific"} vehicles", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check city restriction
        if (promoCode.CityId.HasValue && request.CityId != promoCode.CityId)
        {
            return new PromoCodeValidationResult(
                false, $"This promo code only applies to {promoCode.City?.Name ?? "specific"} locations", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check minimum booking amount
        if (promoCode.MinimumBookingAmount.HasValue && request.BookingAmount < promoCode.MinimumBookingAmount.Value)
        {
            return new PromoCodeValidationResult(
                false, $"Minimum booking amount of GHS {promoCode.MinimumBookingAmount.Value:F2} required", null, request.BookingAmount, 0, request.BookingAmount, "");
        }

        // Check user usage limit (skip for guest users)
        if (!isGuest)
        {
            var userUsageCount = await _db.PromoCodeUsage
                .CountAsync(u => u.PromoCodeId == promoCode.Id && u.UsedByUserId == userId);

            if (userUsageCount >= promoCode.MaxUsesPerUser)
            {
                return new PromoCodeValidationResult(
                    false, $"You have already used this promo code {promoCode.MaxUsesPerUser} time(s)", null, request.BookingAmount, 0, request.BookingAmount, "");
            }
        }
        // For guest users, skip usage limit check - will be validated during booking creation

        // Calculate discount
        decimal discountAmount = promoCode.PromoType switch
        {
            PromoCodeType.Percentage => (request.BookingAmount * promoCode.DiscountValue) / 100,
            PromoCodeType.FixedAmount => promoCode.DiscountValue,
            PromoCodeType.FreeAddon => promoCode.DiscountValue, // Value of the addon
            PromoCodeType.CommissionReduction => 0, // Calculated at payout time
            _ => 0
        };

        // Apply maximum discount cap
        if (promoCode.MaximumDiscountAmount.HasValue && discountAmount > promoCode.MaximumDiscountAmount.Value)
        {
            discountAmount = promoCode.MaximumDiscountAmount.Value;
        }

        var finalAmount = Math.Max(0, request.BookingAmount - discountAmount);

        var promoDetails = new PromoCodeDetailsDto(
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

        return new PromoCodeValidationResult(
            true,
            null,
            promoDetails,
            request.BookingAmount,
            discountAmount,
            finalAmount,
            promoCode.AppliesTo.ToString()
        );
    }

    public async Task<PromoCodeUsage> ApplyPromoCodeAsync(
        string code,
        Guid userId,
        Guid? bookingId,
        decimal originalAmount,
        string userType)
    {
        var promoCode = await _db.PromoCodes
            .Include(p => p.Referrer)
            .FirstOrDefaultAsync(p => p.Code.ToUpper() == code.ToUpper());

        if (promoCode == null)
            throw new InvalidOperationException("Promo code not found");

        // Calculate discount
        decimal discountAmount = promoCode.PromoType switch
        {
            PromoCodeType.Percentage => (originalAmount * promoCode.DiscountValue) / 100,
            PromoCodeType.FixedAmount => promoCode.DiscountValue,
            PromoCodeType.FreeAddon => promoCode.DiscountValue,
            PromoCodeType.CommissionReduction => 0, // Calculated at payout time
            _ => 0
        };

        // Apply maximum discount cap
        if (promoCode.MaximumDiscountAmount.HasValue && discountAmount > promoCode.MaximumDiscountAmount.Value)
        {
            discountAmount = promoCode.MaximumDiscountAmount.Value;
        }

        var finalAmount = Math.Max(0, originalAmount - discountAmount);

        var usage = new PromoCodeUsage
        {
            PromoCodeId = promoCode.Id,
            Code = promoCode.Code,
            UsedByUserId = userId,
            UserType = userType,
            BookingId = bookingId,
            OriginalAmount = originalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            AppliedTo = promoCode.AppliesTo.ToString(),
            ReferrerUserId = promoCode.ReferrerUserId,
            ReferrerRewardAmount = promoCode.ReferralRewardValue,
            ReferrerRewardApplied = false
        };

        _db.PromoCodeUsage.Add(usage);

        // Increment usage count
        promoCode.CurrentTotalUses++;
        promoCode.UpdatedAt = DateTime.UtcNow;

        // Create referral record if this is a referral code and first use
        if (promoCode.IsReferralCode && promoCode.ReferrerUserId.HasValue)
        {
            var existingReferral = await _db.UserReferrals
                .FirstOrDefaultAsync(r => r.ReferrerUserId == promoCode.ReferrerUserId && r.ReferredUserId == userId);

            if (existingReferral == null)
            {
                var referral = new UserReferral
                {
                    ReferrerUserId = promoCode.ReferrerUserId.Value,
                    ReferredUserId = userId,
                    ReferralCode = promoCode.Code,
                    ReferralType = userType,
                    Status = "active"
                };
                _db.UserReferrals.Add(referral);
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Promo code {Code} applied for user {UserId}, discount: {Discount}", 
            code, userId, discountAmount);

        return usage;
    }

    public async Task<string> GenerateReferralCodeAsync(Guid userId, GenerateReferralCodeDto request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Generate code or use custom
        string code;
        if (!string.IsNullOrWhiteSpace(request.CustomCode))
        {
            code = request.CustomCode.ToUpper();
            // Check if code already exists
            if (await _db.PromoCodes.AnyAsync(p => p.Code == code))
                throw new InvalidOperationException("This code is already in use");
        }
        else
        {
            // Auto-generate: FIRSTNAME + random 4 digits
            var baseName = user.FirstName?.ToUpper() ?? "USER";
            var random = new Random();
            do
            {
                code = $"{baseName}{random.Next(1000, 9999)}";
            } while (await _db.PromoCodes.AnyAsync(p => p.Code == code));
        }

        var validUntil = request.ValidUntil ?? DateTime.UtcNow.AddYears(1);

        var promoCode = new PromoCode
        {
            Code = code,
            Description = $"Referral code from {user.FirstName} {user.LastName}",
            PromoType = PromoCodeType.Percentage, // Default for referrals
            DiscountValue = 10, // 10% off for referred user
            TargetUserType = TargetUserType.Both,
            AppliesTo = DiscountAppliesTo.TotalAmount,
            ValidFrom = DateTime.UtcNow,
            ValidUntil = validUntil,
            MaxUsesPerUser = 1,
            IsActive = true,
            CreatedBy = "owner",
            CreatedByUserId = userId,
            FirstTimeUsersOnly = true,
            IsReferralCode = true,
            ReferrerUserId = userId,
            ReferralRewardType = request.RewardType,
            ReferralRewardValue = request.RewardValue
        };

        _db.PromoCodes.Add(promoCode);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Referral code {Code} generated for user {UserId}", code, userId);

        return code;
    }

    public async Task<UserReferralStatsDto> GetUserReferralStatsAsync(Guid userId)
    {
        var referrals = await _db.UserReferrals
            .Include(r => r.ReferredUser)
            .Where(r => r.ReferrerUserId == userId)
            .ToListAsync();

        var totalRewards = await _db.PromoCodeUsage
            .Where(u => u.ReferrerUserId == userId && u.ReferrerRewardApplied)
            .SumAsync(u => u.ReferrerRewardAmount ?? 0);

        var referredUsers = referrals.Select(r => new ReferredUserDto(
            r.ReferredUserId,
            $"{r.ReferredUser.FirstName} {r.ReferredUser.LastName}",
            r.ReferredUser.Email,
            r.ReferralType,
            r.TotalRewardEarned,
            r.TotalBookingsFromReferred,
            r.CreatedAt,
            r.Status
        )).ToList();

        return new UserReferralStatsDto(
            referrals.Count,
            referrals.Count(r => r.Status == "active"),
            totalRewards,
            referrals.Sum(r => r.TotalBookingsFromReferred),
            referredUsers
        );
    }

    public async Task ProcessReferralRewardAsync(Guid promoCodeUsageId)
    {
        var usage = await _db.PromoCodeUsage
            .Include(u => u.PromoCode)
            .FirstOrDefaultAsync(u => u.Id == promoCodeUsageId);

        if (usage == null || usage.ReferrerRewardApplied || !usage.ReferrerUserId.HasValue)
            return;

        var referral = await _db.UserReferrals
            .FirstOrDefaultAsync(r => 
                r.ReferrerUserId == usage.ReferrerUserId && 
                r.ReferredUserId == usage.UsedByUserId);

        if (referral != null)
        {
            referral.TotalRewardEarned += usage.ReferrerRewardAmount ?? 0;
            referral.TotalBookingsFromReferred++;
            referral.UpdatedAt = DateTime.UtcNow;
        }

        usage.ReferrerRewardApplied = true;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Referral reward processed for usage {UsageId}", promoCodeUsageId);
    }
}
