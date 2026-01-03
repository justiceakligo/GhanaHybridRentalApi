using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhanaHybridRentalApi.Models;

public enum PromoCodeType
{
    Percentage,           // 10%, 20% off
    FixedAmount,         // GHS 50, GHS 100 off
    FreeAddon,           // Free protection plan, free delivery
    CommissionReduction, // Reduce platform fee for owners
    OwnerVehicleDiscount // Owner-created discount for their vehicle (reduces their earnings)
}

public enum TargetUserType
{
    Renter,
    Owner,
    Both
}

public enum DiscountAppliesTo
{
    TotalAmount,      // Applied to total booking cost (renters)
    PlatformFee,      // Applied to platform fee only
    ProtectionPlan,   // Free or discounted protection plan
    RentalAmount,     // Applied to base rental amount (owner vehicle discounts reduce owner earnings)
    Commission        // Reduce commission for owners
}

public enum ReferralRewardType
{
    Credit,              // Store credit
    CommissionReduction, // Reduce commission
    Cash                // Cash payout
}

public class PromoCode
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public PromoCodeType PromoType { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountValue { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public TargetUserType TargetUserType { get; set; }

    [Required]
    [Column(TypeName = "varchar(50)")]
    public DiscountAppliesTo AppliesTo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MinimumBookingAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MaximumDiscountAmount { get; set; }

    [Required]
    public DateTime ValidFrom { get; set; }

    [Required]
    public DateTime ValidUntil { get; set; }

    public int? MaxTotalUses { get; set; }

    [Required]
    public int MaxUsesPerUser { get; set; } = 1;

    [Required]
    public int CurrentTotalUses { get; set; } = 0;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = "admin"; // admin, owner, system

    public Guid? CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public Guid? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public CarCategory? Category { get; set; }

    public Guid? CityId { get; set; }
    [ForeignKey(nameof(CityId))]
    public City? City { get; set; }

    public Guid? VehicleId { get; set; }
    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    [Required]
    public bool FirstTimeUsersOnly { get; set; } = false;

    [Required]
    public bool IsReferralCode { get; set; } = false;

    public Guid? ReferrerUserId { get; set; }
    [ForeignKey(nameof(ReferrerUserId))]
    public User? Referrer { get; set; }

    [Column(TypeName = "varchar(50)")]
    public ReferralRewardType? ReferralRewardType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ReferralRewardValue { get; set; }

    public string? MetadataJson { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PromoCodeUsage> Usages { get; set; } = new List<PromoCodeUsage>();
}

public class PromoCodeUsage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PromoCodeId { get; set; }
    [ForeignKey(nameof(PromoCodeId))]
    public PromoCode PromoCode { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid UsedByUserId { get; set; }
    [ForeignKey(nameof(UsedByUserId))]
    public User UsedByUser { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string UserType { get; set; } = string.Empty; // renter, owner

    public Guid? BookingId { get; set; }
    [ForeignKey(nameof(BookingId))]
    public Booking? Booking { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal OriginalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal FinalAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public string AppliedTo { get; set; } = string.Empty;

    public Guid? ReferrerUserId { get; set; }
    [ForeignKey(nameof(ReferrerUserId))]
    public User? Referrer { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ReferrerRewardAmount { get; set; }

    [Required]
    public bool ReferrerRewardApplied { get; set; } = false;

    [Required]
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}

public class UserReferral
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ReferrerUserId { get; set; }
    [ForeignKey(nameof(ReferrerUserId))]
    public User Referrer { get; set; } = null!;

    [Required]
    public Guid ReferredUserId { get; set; }
    [ForeignKey(nameof(ReferredUserId))]
    public User ReferredUser { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ReferralCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ReferralType { get; set; } = string.Empty; // renter, owner

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalRewardEarned { get; set; } = 0;

    [Required]
    public int TotalBookingsFromReferred { get; set; } = 0;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, inactive, completed

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
