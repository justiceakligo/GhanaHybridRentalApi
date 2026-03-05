using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Represents an insurance policy issued for a booking
/// For countries requiring real insurance, this stores the actual policy from the provider
/// For countries with mock insurance, this stores protection plan details
/// </summary>
public class InsurancePolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the booking this policy covers
    /// </summary>
    public Guid BookingId { get; set; }
    
    /// <summary>
    /// Reference to the protection plan selected
    /// </summary>
    public Guid? ProtectionPlanId { get; set; }
    
    /// <summary>
    /// Country code where this policy applies
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Policy number from insurance provider
    /// For real insurance: Actual policy number (e.g., "IC-2026-789456")
    /// For mock: Generated reference (e.g., "PROT-A1B2C3D4")
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string PolicyNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of insurance provider
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string InsuranceProviderName { get; set; } = string.Empty;
    
    /// <summary>
    /// When coverage starts
    /// </summary>
    public DateTime CoverageStartDate { get; set; }
    
    /// <summary>
    /// When coverage ends
    /// </summary>
    public DateTime CoverageEndDate { get; set; }
    
    /// <summary>
    /// Premium amount charged (in local currency)
    /// </summary>
    public decimal PremiumAmount { get; set; }
    
    /// <summary>
    /// Liability coverage amount (in local currency)
    /// </summary>
    public decimal? LiabilityCoverage { get; set; }
    
    /// <summary>
    /// URL to the certificate PDF
    /// </summary>
    [MaxLength(500)]
    public string? CertificateUrl { get; set; }
    
    /// <summary>
    /// Full policy details from provider API (JSON)
    /// Stores complete response for audit/claims
    /// </summary>
    public string? ProviderPolicyJson { get; set; }
    
    /// <summary>
    /// Policy status: issued, active, expired, cancelled
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "issued";
    
    /// <summary>
    /// When the policy was issued
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Booking? Booking { get; set; }
    public ProtectionPlan? ProtectionPlan { get; set; }
}
