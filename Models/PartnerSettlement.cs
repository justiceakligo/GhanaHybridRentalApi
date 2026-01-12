using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Tracks financial settlements with integration partners.
/// Separates customer payment tracking from partner-to-platform settlements.
/// </summary>
public class PartnerSettlement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IntegrationPartnerId { get; set; }
    public Guid BookingId { get; set; }

    /// <summary>
    /// Settlement period (for batch settlements)
    /// </summary>
    public DateTime SettlementPeriodStart { get; set; }
    public DateTime SettlementPeriodEnd { get; set; }

    [MaxLength(64)]
    public string? BookingReference { get; set; }

    /// <summary>
    /// Total amount customer paid to partner
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Partner's commission percentage
    /// </summary>
    public decimal CommissionPercent { get; set; }

    /// <summary>
    /// Partner's commission amount (what partner keeps)
    /// </summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>
    /// Settlement amount (what partner owes us = TotalAmount - CommissionAmount)
    /// </summary>
    public decimal SettlementAmount { get; set; }

    /// <summary>
    /// Currency for settlement amounts (e.g., GHS)
    /// </summary>
    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    /// <summary>
    /// Settlement status: pending, paid, overdue, cancelled
    /// </summary>
    [MaxLength(32)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// When settlement is due (based on partner's payment terms)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// When partner actually paid us
    /// </summary>
    public DateTime? PaidDate { get; set; }

    /// <summary>
    /// Partner's payment transaction reference
    /// </summary>
    [MaxLength(256)]
    public string? PaymentReference { get; set; }

    /// <summary>
    /// How partner paid (bank transfer, stripe, etc.)
    /// </summary>
    [MaxLength(64)]
    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public IntegrationPartner? IntegrationPartner { get; set; }
    public Booking? Booking { get; set; }
}
