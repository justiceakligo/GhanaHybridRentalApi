using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Review and rating for vehicles, drivers, or overall service
/// </summary>
public class Review
{
    public Guid Id { get; set; }

    [Required]
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    [Required]
    public Guid ReviewerUserId { get; set; }
    public User? ReviewerUser { get; set; }

    /// <summary>
    /// What is being reviewed: "vehicle", "driver", "service"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the vehicle or driver being reviewed
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// Rating from 1-5
    /// </summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    /// <summary>
    /// Moderation status: "pending", "approved", "rejected", "flagged"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ModerationStatus { get; set; } = "pending";

    public Guid? ModeratedByUserId { get; set; }
    public User? ModeratedByUser { get; set; }

    [StringLength(500)]
    public string? ModerationNotes { get; set; }

    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// Whether the review is publicly visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
