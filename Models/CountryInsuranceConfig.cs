using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Configures insurance requirements and providers for each country
/// </summary>
public class CountryInsuranceConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the Country
    /// </summary>
    public Guid CountryId { get; set; }
    
    /// <summary>
    /// Whether this country requires real insurance policy via API
    /// False = Use mock/internal protection plans (Ghana)
    /// True = Must call real insurance provider API (Canada)
    /// </summary>
    public bool RequiresRealInsurance { get; set; } = false;
    
    /// <summary>
    /// Name of the insurance provider (e.g., "Intact Insurance", "Enterprise Insurance")
    /// </summary>
    [MaxLength(255)]
    public string? InsuranceProviderName { get; set; }
    
    /// <summary>
    /// API URL for insurance provider (if RequiresRealInsurance = true)
    /// </summary>
    [MaxLength(500)]
    public string? InsuranceProviderApiUrl { get; set; }
    
    /// <summary>
    /// Whether to automatically issue policy on booking creation
    /// </summary>
    public bool AutoIssuePolicy { get; set; } = false;
    
    /// <summary>
    /// Minimum liability coverage amount in local currency
    /// </summary>
    public decimal? MinimumLiabilityAmount { get; set; }
    
    /// <summary>
    /// Additional configuration (JSON)
    /// Can include: certificate template, regulatory info, etc.
    /// </summary>
    public string? ConfigJson { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Country? Country { get; set; }
}
