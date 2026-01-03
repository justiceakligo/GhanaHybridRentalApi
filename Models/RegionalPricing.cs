using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Regional pricing rules for vehicles
/// </summary>
public class RegionalPricing
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Region { get; set; } = string.Empty; // e.g., "Greater Accra", "Ashanti", "Northern"

    [StringLength(100)]
    public string? City { get; set; } // e.g., "Accra", "Kumasi", "Tamale"

    public Guid? CategoryId { get; set; }
    public CarCategory? Category { get; set; }

    /// <summary>
    /// Price multiplier for this region (e.g., 1.2 = 20% more expensive)
    /// </summary>
    [Range(0.1, 10.0)]
    public decimal PriceMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Extra hold/deposit amount for this region
    /// </summary>
    [Range(0, 100000)]
    public decimal ExtraHoldAmount { get; set; } = 0m;

    /// <summary>
    /// Minimum daily rate override for this region (optional)
    /// </summary>
    public decimal? MinDailyRate { get; set; }

    /// <summary>
    /// Maximum daily rate override for this region (optional)
    /// </summary>
    public decimal? MaxDailyRate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
