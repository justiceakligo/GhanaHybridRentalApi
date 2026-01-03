using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? BookingId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(32)]
    public string Type { get; set; } = "payment"; // payment, refund, payout, deposit

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, completed, failed, cancelled

    public decimal Amount { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(32)]
    public string Method { get; set; } = "momo"; // momo, card, bank

    [MaxLength(256)]
    public string? ExternalTransactionId { get; set; }

    [MaxLength(256)]
    public string? Reference { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    // New: typed captured amount persisted when admin captures a pending payment
    public decimal? CapturedAmount { get; set; }

    public Booking? Booking { get; set; }
    public User? User { get; set; }
}
