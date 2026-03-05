using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

/// <summary>
/// Scoped service that provides country context for the current request
/// </summary>
public class CountryContext : ICountryContext
{
    private Country? _country;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CountryContext> _logger;

    // Default values for Ghana (fallback)
    private const string DefaultCountryCode = "GH";
    private const string DefaultCountryName = "Ghana";
    private const string DefaultCurrency = "GHS";
    private const string DefaultCurrencySymbol = "₵";
    private const string DefaultPhoneCode = "+233";
    private const string DefaultTimezone = "Africa/Accra";
    private const string DefaultLanguage = "en-GH";

    public CountryContext(IServiceProvider serviceProvider, ILogger<CountryContext> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private async Task<Country> GetCountryAsync()
    {
        if (_country != null)
            return _country;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Try to get country from HttpContext
        var httpContext = scope.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
        var countryCode = httpContext?.Items["CountryCode"] as string ?? DefaultCountryCode;

        _country = await db.Countries
            .FirstOrDefaultAsync(c => c.Code == countryCode.ToUpper() && c.IsActive);

        if (_country == null)
        {
            _logger.LogWarning("Country {CountryCode} not found or inactive. Using defaults.", countryCode);
            
            // Return default Ghana country object
            _country = new Country
            {
                Code = DefaultCountryCode,
                Name = DefaultCountryName,
                CurrencyCode = DefaultCurrency,
                CurrencySymbol = DefaultCurrencySymbol,
                PhoneCode = DefaultPhoneCode,
                Timezone = DefaultTimezone,
                DefaultLanguage = DefaultLanguage,
                IsActive = true,
                IsDefault = true,
                PaymentProvidersJson = JsonSerializer.Serialize(new[] { "paystack", "stripe" })
            };
        }

        return _country;
    }

    public string CountryCode => _country?.Code ?? DefaultCountryCode;

    public string CountryName => _country?.Name ?? DefaultCountryName;

    public string CurrencyCode => _country?.CurrencyCode ?? DefaultCurrency;

    public string CurrencySymbol => _country?.CurrencySymbol ?? DefaultCurrencySymbol;

    public string PhoneCode => _country?.PhoneCode ?? DefaultPhoneCode;

    public string Timezone => _country?.Timezone ?? DefaultTimezone;

    public string DefaultLanguage => _country?.DefaultLanguage ?? DefaultLanguage;

    public bool IsActive => _country?.IsActive ?? true;

    public T? GetConfig<T>(string key) where T : class
    {
        var country = GetCountryAsync().GetAwaiter().GetResult();
        
        if (string.IsNullOrEmpty(country.ConfigJson))
            return null;

        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(country.ConfigJson);
            if (config != null && config.TryGetValue(key, out var value))
            {
                return JsonSerializer.Deserialize<T>(value.GetRawText());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing country config for key {Key}", key);
        }

        return null;
    }

    public List<string> GetEnabledPaymentProviders()
    {
        var country = GetCountryAsync().GetAwaiter().GetResult();
        
        if (string.IsNullOrEmpty(country.PaymentProvidersJson))
            return new List<string> { "paystack" };

        try
        {
            return JsonSerializer.Deserialize<List<string>>(country.PaymentProvidersJson) 
                   ?? new List<string> { "paystack" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing payment providers");
            return new List<string> { "paystack" };
        }
    }
}
