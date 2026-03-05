namespace GhanaHybridRentalApi.Services.Insurance;

/// <summary>
/// Interface for insurance providers (real or mock)
/// </summary>
public interface IInsuranceProvider
{
    /// <summary>
    /// Create a new insurance policy
    /// </summary>
    Task<PolicyResponse> CreatePolicyAsync(PolicyRequest request);
    
    /// <summary>
    /// Cancel an existing policy
    /// </summary>
    Task<bool> CancelPolicyAsync(string policyNumber);
    
    /// <summary>
    /// Get policy details
    /// </summary>
    Task<PolicyResponse?> GetPolicyAsync(string policyNumber);
}

/// <summary>
/// Factory to get the right insurance provider for a country
/// </summary>
public interface IInsuranceProviderFactory
{
    IInsuranceProvider GetProvider(string countryCode);
}

/// <summary>
/// Main insurance orchestrator service
/// Handles insurance for all bookings
/// </summary>
public interface IInsuranceOrchestrator
{
    /// <summary>
    /// Handle insurance for a new booking
    /// - Checks if country requires real insurance
    /// - Issues policy if needed
    /// - Generates certificate
    /// </summary>
    Task HandleBookingInsuranceAsync(Guid bookingId);
    
    /// <summary>
    /// Get insurance policy for a booking
    /// </summary>
    Task<Models.InsurancePolicy?> GetBookingPolicyAsync(Guid bookingId);
}

/// <summary>
/// Certificate generation service
/// </summary>
public interface ICertificateGenerator
{
    /// <summary>
    /// Generate insurance certificate PDF for a booking
    /// </summary>
    Task<string> GenerateCertificateAsync(Guid bookingId);
}
