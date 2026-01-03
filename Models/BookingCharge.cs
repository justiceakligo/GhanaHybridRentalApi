using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class BookingCharge
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid ChargeTypeId { get; set; }
    public PostRentalChargeType ChargeType { get; set; } = null!;

    public decimal Amount { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(128)]
    public string? Label { get; set; }

    public string? Notes { get; set; }

    // Evidence: URLs of photos, enforced as required in API.
    public string EvidencePhotoUrlsJson { get; set; } = "[]";

    // Status workflow:
    // owner creates: pending_review
    // admin: approved -> then paid or rejected/waived
    [MaxLength(32)]
    public string Status { get; set; } = "pending_review";

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }

    public Guid? PaymentTransactionId { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}
