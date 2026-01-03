using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class PostRentalChargeType
{
    public Guid Id { get; set; }

    [MaxLength(64)]
    public string Code { get; set; } = null!; // e.g. "no_smoking", "traffic_fine"

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Fixed amount set by admin
    public decimal DefaultAmount { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    [MaxLength(32)]
    public string RecipientType { get; set; } = "owner"; // or "platform"

    public bool IsActive { get; set; } = true;

    // Admin can change this; owners never set amount.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
