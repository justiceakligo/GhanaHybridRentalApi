using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class InstantWithdrawal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal FeeAmount { get; set; }

    public decimal FeePercentage { get; set; } // e.g., 3.0 for 3%

    public decimal NetAmount { get; set; } // Amount - FeeAmount

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed

    [MaxLength(32)]
    public string Method { get; set; } = "momo"; // momo, bank

    [MaxLength(256)]
    public string? ExternalTransferId { get; set; } // Paystack transfer ID

    [MaxLength(256)]
    public string? Reference { get; set; }

    public string? PayoutDetailsJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
