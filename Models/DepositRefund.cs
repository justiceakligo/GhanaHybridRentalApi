using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class DepositRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public decimal Amount { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed, cancelled

    [MaxLength(32)]
    public string PaymentMethod { get; set; } = "momo"; // momo, card - copied from original booking

    [MaxLength(256)]
    public string? ExternalRefundId { get; set; } // Paystack refund ID

    [MaxLength(256)]
    public string? Reference { get; set; }

    public string? RefundDetailsJson { get; set; }

    public Guid? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? DueDate { get; set; } // 2 days after vehicle return

    public bool AdminNotified { get; set; } = false;

    public DateTime? AdminNotifiedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Notes { get; set; }
}
