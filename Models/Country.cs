using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

/// <summary>
/// Represents a country where the rental service operates
/// </summary>
public class Country
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "GH", "NG", "KE")
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Country name (e.g., "Ghana", "Nigeria", "Kenya")
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Currency code ISO 4217 (e.g., "GHS", "NGN", "KES")
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency symbol (e.g., "₵", "₦", "KSh")
    /// </summary>
    [MaxLength(10)]
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>
    /// Phone country code (e.g., "+233", "+234", "+254")
    /// </summary>
    [MaxLength(10)]
    public string PhoneCode { get; set; } = string.Empty;

    /// <summary>
    /// Default timezone (e.g., "Africa/Accra", "Africa/Lagos", "Africa/Nairobi")
    /// </summary>
    [MaxLength(64)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Default language code (e.g., "en-GH", "en-NG", "sw-KE")
    /// </summary>
    [MaxLength(10)]
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Whether this country is currently active for operations
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the default country (for routes without country prefix)
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Enabled payment providers (JSON array: ["paystack", "stripe", "flutterwave"])
    /// </summary>
    public string? PaymentProvidersJson { get; set; }

    /// <summary>
    /// Country-specific configuration (JSON object for flexibility)
    /// </summary>
    public string? ConfigJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<City>? Cities { get; set; }
}
