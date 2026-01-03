using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// User reports for inappropriate behavior, violations, etc.
/// </summary>
public class Report
{
    public Guid Id { get; set; }

    [Required]
    public Guid ReporterUserId { get; set; }
    public User? ReporterUser { get; set; }

    /// <summary>
    /// What is being reported: "user", "vehicle", "booking", "review"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being reported
    /// </summary>
    [Required]
    public Guid TargetId { get; set; }

    /// <summary>
    /// Reason for report: "inappropriate_content", "fraud", "harassment", "spam", "safety_concern", "other"
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Status: "pending", "under_review", "resolved", "dismissed"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Admin action taken: "none", "warning", "suspension", "ban", "content_removed"
    /// </summary>
    [StringLength(50)]
    public string? ActionTaken { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }

    [StringLength(1000)]
    public string? AdminNotes { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
