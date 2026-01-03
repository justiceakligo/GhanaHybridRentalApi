using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Payout
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed

    [MaxLength(32)]
    public string Method { get; set; } = "momo"; // momo, bank

    [MaxLength(256)]
    public string? ExternalPayoutId { get; set; }

    [MaxLength(256)]
    public string? Reference { get; set; }

    public string? PayoutDetailsJson { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string? BookingIdsJson { get; set; }

    public User? Owner { get; set; }
}
