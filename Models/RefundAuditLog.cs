using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class RefundAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DepositRefundId { get; set; }
    public DepositRefund DepositRefund { get; set; } = null!;

    [MaxLength(64)]
    public string Action { get; set; } = null!; // created, processing, completed, failed, admin_notified, cancelled

    [MaxLength(32)]
    public string OldStatus { get; set; } = null!;

    [MaxLength(32)]
    public string NewStatus { get; set; } = null!;

    public Guid? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }

    public string? Notes { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
