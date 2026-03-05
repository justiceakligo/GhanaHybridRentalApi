namespace GhanaHybridRentalApi.Services;

/// <summary>
/// Provides country-specific context for the current request
/// </summary>
public interface ICountryContext
{
    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "GH", "NG", "KE")
    /// </summary>
    string CountryCode { get; }

    /// <summary>
    /// Country name (e.g., "Ghana", "Nigeria", "Kenya")
    /// </summary>
    string CountryName { get; }

    /// <summary>
    /// Currency code ISO 4217 (e.g., "GHS", "NGN", "KES")
    /// </summary>
    string CurrencyCode { get; }

    /// <summary>
    /// Currency symbol (e.g., "₵", "₦", "KSh")
    /// </summary>
    string CurrencySymbol { get; }

    /// <summary>
    /// Phone country code (e.g., "+233", "+234", "+254")
    /// </summary>
    string PhoneCode { get; }

    /// <summary>
    /// Default timezone for the country
    /// </summary>
    string Timezone { get; }

    /// <summary>
    /// Default language code
    /// </summary>
    string DefaultLanguage { get; }

    /// <summary>
    /// Whether the country is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Get country-specific configuration value
    /// </summary>
    T? GetConfig<T>(string key) where T : class;

    /// <summary>
    /// Get enabled payment providers for this country
    /// </summary>
    List<string> GetEnabledPaymentProviders();
}
