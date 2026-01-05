using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhanaHybridRentalApi.Models;

public class OwnerProfile
{
    [Key]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [MaxLength(32)]
    public string OwnerType { get; set; } = "individual"; // individual | business

    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(256)]
    public string? CompanyName { get; set; }

    [MaxLength(128)]
    public string? BusinessRegistrationNumber { get; set; }

    [MaxLength(32)]
    public string PayoutPreference { get; set; } = "momo"; // momo | bank

    public string? PayoutDetailsJson { get; set; }

    public string? PayoutDetailsPendingJson { get; set; }

    [MaxLength(32)]
    public string PayoutVerificationStatus { get; set; } = "unverified"; // unverified|pending|verified|rejected

    // Payout scheduling settings
    [MaxLength(32)]
    public string PayoutFrequency { get; set; } = "weekly"; // daily, weekly, biweekly, monthly

    public decimal MinimumPayoutAmount { get; set; } = 50.0m; // Minimum amount before payout is processed

    public bool InstantWithdrawalEnabled { get; set; } = true; // Allow owner to request instant withdrawals

    [MaxLength(32)]
    public string CompanyVerificationStatus { get; set; } = "unverified";

    [MaxLength(256)]
    public string? CompanyNamePending { get; set; }

    [MaxLength(128)]
    public string? BusinessRegistrationNumberPending { get; set; }

    // Contact and location information for renters
    [MaxLength(32)]
    public string? BusinessPhone { get; set; }

    public string? BusinessAddress { get; set; }

    [MaxLength(128)]
    public string? GpsAddress { get; set; } // Ghana GPS address (e.g., GA-123-4567)

    public string? PickupInstructions { get; set; }

    [MaxLength(128)]
    public string? City { get; set; }

    [MaxLength(128)]
    public string? Region { get; set; }

    public string? Bio { get; set; }

    public string? NotificationPreferencesJson { get; set; }

    public User? User { get; set; }
}
