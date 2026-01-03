using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class ProfileChangeAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [MaxLength(128)]
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
