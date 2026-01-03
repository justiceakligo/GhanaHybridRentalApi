using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class NotificationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Optional link to a booking
    public Guid? BookingId { get; set; }

    // Target user for in-app notification (optional)
    public Guid? TargetUserId { get; set; }

    // Fallback contact channels for guests
    [MaxLength(256)]
    public string? TargetEmail { get; set; }

    [MaxLength(32)]
    public string? TargetPhone { get; set; }

    // Channels to deliver to (serialized JSON array: ["inApp","email","whatsapp","sms","push"]) 
    public string ChannelsJson { get; set; } = "[]";

    [MaxLength(256)]
    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? TemplateName { get; set; }

    // Arbitrary metadata (JSON)
    public string? MetadataJson { get; set; }

    // Scheduling
    public DateTime? ScheduledAt { get; set; }
    public bool SendImmediately { get; set; } = true;

    // Status: pending | queued | sent | failed | cancelled
    [MaxLength(32)]
    public string Status { get; set; } = "pending";

    public int Attempts { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}