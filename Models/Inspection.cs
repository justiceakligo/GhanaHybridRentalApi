using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Inspection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BookingId { get; set; }

    [MaxLength(16)]
    public string Type { get; set; } = "pickup"; // pickup | return

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    [MaxLength(256)]
    public string? MagicLinkToken { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? PhotosJson { get; set; }

    public int? Mileage { get; set; }

    [MaxLength(32)]
    public string? FuelLevel { get; set; }

    public string? DamageNotesJson { get; set; }

    public Booking? Booking { get; set; }
}
