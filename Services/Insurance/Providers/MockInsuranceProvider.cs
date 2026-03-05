namespace GhanaHybridRentalApi.Services.Insurance.Providers;

/// <summary>
/// Mock insurance provider for countries that don't require real insurance (Ghana, etc.)
/// Returns fake policy data without calling any external API
/// </summary>
public class MockInsuranceProvider : IInsuranceProvider
{
    private readonly ILogger<MockInsuranceProvider> _logger;

    public MockInsuranceProvider(ILogger<MockInsuranceProvider> logger)
    {
        _logger = logger;
    }

    public Task<PolicyResponse> CreatePolicyAsync(PolicyRequest request)
    {
        _logger.LogInformation("Mock insurance provider - creating policy for booking {BookingId}", 
            request.BookingId);

        // Generate mock policy number
        var policyNumber = $"PROT-{request.BookingId.ToString().Substring(0, 8).ToUpper()}";

        var response = new PolicyResponse
        {
            PolicyNumber = policyNumber,
            StartDate = request.CoverageStart,
            EndDate = request.CoverageEnd,
            PremiumAmount = 0, // No additional cost for mock
            LiabilityCoverage = 500000, // Default coverage
            Status = "active",
            CertificateUrl = null,
            AdditionalDetails = new Dictionary<string, object>
            {
                ["provider"] = "RyveRental Protection",
                ["type"] = "damage_waiver",
                ["note"] = "Vehicle owner maintains comprehensive insurance coverage"
            }
        };

        return Task.FromResult(response);
    }

    public Task<bool> CancelPolicyAsync(string policyNumber)
    {
        _logger.LogInformation("Mock insurance provider - cancelling policy {PolicyNumber}", 
            policyNumber);
        return Task.FromResult(true);
    }

    public Task<PolicyResponse?> GetPolicyAsync(string policyNumber)
    {
        _logger.LogInformation("Mock insurance provider - getting policy {PolicyNumber}", 
            policyNumber);
        
        // Return null or mock data
        return Task.FromResult<PolicyResponse?>(null);
    }
}
