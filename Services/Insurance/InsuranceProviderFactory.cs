using GhanaHybridRentalApi.Services.Insurance.Providers;

namespace GhanaHybridRentalApi.Services.Insurance;

/// <summary>
/// Factory that returns the appropriate insurance provider for each country
/// </summary>
public class InsuranceProviderFactory : IInsuranceProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InsuranceProviderFactory> _logger;

    public InsuranceProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<InsuranceProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IInsuranceProvider GetProvider(string countryCode)
    {
        _logger.LogDebug("Getting insurance provider for country: {CountryCode}", countryCode);

        return countryCode.ToUpper() switch
        {
            "GH" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
            "NG" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
            "KE" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
            "ZA" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
            "TZ" => _serviceProvider.GetRequiredService<MockInsuranceProvider>(),
            "CA" => _serviceProvider.GetRequiredService<CanadaInsuranceProvider>(),
            
            // Default to mock for unknown countries
            _ => _serviceProvider.GetRequiredService<MockInsuranceProvider>()
        };
    }
}
