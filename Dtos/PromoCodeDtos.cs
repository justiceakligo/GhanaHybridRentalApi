using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Dtos;

// Admin: Create promo code
public record CreatePromoCodeDto(
    string Code,
    string? Description,
    PromoCodeType PromoType,
    decimal DiscountValue,
    TargetUserType TargetUserType,
    DiscountAppliesTo AppliesTo,
    decimal? MinimumBookingAmount,
    decimal? MaximumDiscountAmount,
    DateTime ValidFrom,
    DateTime ValidUntil,
    int? MaxTotalUses,
    int MaxUsesPerUser,
    bool FirstTimeUsersOnly,
    Guid? VehicleId,
    Guid? CategoryId,
    Guid? CityId
);

// Owner: Create vehicle-specific promo code
public record CreateOwnerVehiclePromoDto(
    Guid VehicleId,
    string? CustomCode,
    string? Description,
    PromoCodeType PromoType, // Percentage or FixedAmount
    decimal DiscountValue,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxTotalUses,
    int MaxUsesPerUser
);

// Admin: Update promo code
public record UpdatePromoCodeDto(
    string? Description,
    decimal? DiscountValue,
    decimal? MinimumBookingAmount,
    decimal? MaximumDiscountAmount,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxTotalUses,
    int? MaxUsesPerUser,
    bool? IsActive
);

// Renter/Owner: Validate promo code
public record ValidatePromoCodeDto(
    string Code,
    decimal BookingAmount,
    Guid? VehicleId,
    Guid? CategoryId,
    Guid? CityId,
    int? TripDuration
);

// Response: Promo code validation result
public record PromoCodeValidationResult(
    bool IsValid,
    string? ErrorMessage,
    PromoCodeDetailsDto? PromoCode,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string AppliesTo
);

// Response: Promo code details
public record PromoCodeDetailsDto(
    Guid Id,
    string Code,
    string? Description,
    string PromoType,
    decimal DiscountValue,
    string TargetUserType,
    string AppliesTo,
    decimal? MinimumBookingAmount,
    decimal? MaximumDiscountAmount,
    DateTime ValidFrom,
    DateTime ValidUntil,
    int? MaxTotalUses,
    int MaxUsesPerUser,
    int CurrentTotalUses,
    bool IsActive,
    bool FirstTimeUsersOnly,
    bool IsReferralCode,
    string? ReferrerName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? CityId,
    string? CityName,
    DateTime CreatedAt
);

// Response: Promo code usage history
public record PromoCodeUsageDto(
    Guid Id,
    string Code,
    string UserEmail,
    string UserType,
    Guid? BookingId,
    string? BookingReference,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string AppliedTo,
    Guid? ReferrerUserId,
    string? ReferrerName,
    decimal? ReferrerRewardAmount,
    bool ReferrerRewardApplied,
    DateTime UsedAt
);

// Owner: Generate referral code
public record GenerateReferralCodeDto(
    string? CustomCode, // Optional custom code, otherwise auto-generated
    ReferralRewardType RewardType,
    decimal RewardValue,
    DateTime? ValidUntil
);

// Response: Referral code details
public record ReferralCodeDto(
    string Code,
    string ReferrerName,
    ReferralRewardType RewardType,
    decimal RewardValue,
    int TotalReferrals,
    decimal TotalRewardsEarned,
    DateTime ValidFrom,
    DateTime ValidUntil,
    bool IsActive
);

// Response: User referral stats
public record UserReferralStatsDto(
    int TotalReferrals,
    int ActiveReferrals,
    decimal TotalRewardsEarned,
    int TotalBookingsFromReferrals,
    List<ReferredUserDto> ReferredUsers
);

public record ReferredUserDto(
    Guid UserId,
    string Name,
    string Email,
    string ReferralType,
    decimal RewardEarned,
    int BookingsCompleted,
    DateTime ReferredAt,
    string Status
);

// Admin: Promo code analytics
public record PromoCodeAnalyticsDto(
    string Code,
    int TotalUses,
    int UniqueUsers,
    decimal TotalDiscountGiven,
    decimal AverageDiscountPerUse,
    int NewCustomersAcquired,
    decimal RevenueGenerated,
    List<UsageByDateDto> UsageByDate
);

public record UsageByDateDto(
    DateTime Date,
    int Uses,
    decimal DiscountGiven
);
