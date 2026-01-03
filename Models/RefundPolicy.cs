using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Refund policy rules for booking cancellations
/// </summary>
public class RefundPolicy
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string PolicyName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Hours before pickup time to qualify for this refund rate
    /// </summary>
    [Range(0, 8760)] // Max 1 year in hours
    public int HoursBeforePickup { get; set; }

    /// <summary>
    /// Percentage of refund (0-100)
    /// </summary>
    [Range(0, 100)]
    public decimal RefundPercentage { get; set; }

    /// <summary>
    /// Whether to refund the deposit as well
    /// </summary>
    public bool RefundDeposit { get; set; } = true;

    /// <summary>
    /// Optional category restriction (null = applies to all categories)
    /// </summary>
    public Guid? CategoryId { get; set; }
    public CarCategory? Category { get; set; }

    /// <summary>
    /// Priority order (lower number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
