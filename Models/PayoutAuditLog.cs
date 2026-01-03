using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class PayoutAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PayoutId { get; set; }
    public Payout? Payout { get; set; }

    [MaxLength(64)]
    public string Action { get; set; } = string.Empty; // processing, completed, failed, created

    [MaxLength(32)]
    public string OldStatus { get; set; } = string.Empty;

    [MaxLength(32)]
    public string NewStatus { get; set; } = string.Empty;

    public Guid? PerformedByUserId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}