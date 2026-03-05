using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services.Insurance;

/// <summary>
/// Main orchestrator service that handles insurance for bookings
/// Determines if real insurance is needed and coordinates policy creation + certificate generation
/// </summary>
public class InsuranceOrchestrator : IInsuranceOrchestrator
{
    private readonly AppDbContext _context;
    private readonly IInsuranceProviderFactory _providerFactory;
    private readonly ICertificateGenerator _certificateGenerator;
    private readonly ILogger<InsuranceOrchestrator> _logger;

    public InsuranceOrchestrator(
        AppDbContext context,
        IInsuranceProviderFactory providerFactory,
        ICertificateGenerator certificateGenerator,
        ILogger<InsuranceOrchestrator> logger)
    {
        _context = context;
        _providerFactory = providerFactory;
        _certificateGenerator = certificateGenerator;
        _logger = logger;
    }

    public async Task HandleBookingInsuranceAsync(int bookingId)
    {
        _logger.LogInformation("Processing insurance for booking {BookingId}", bookingId);

        try
        {
            // Load booking with all related data
            var booking = await _context.Bookings
                .Include(b => b.Vehicle)
                .Include(b => b.Renter)
                .Include(b => b.ProtectionPlan)
                .Include(b => b.City)
                    .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                _logger.LogError("Booking {BookingId} not found", bookingId);
                return;
            }

            if (booking.City?.Country == null)
            {
                _logger.LogWarning("Booking {BookingId} has no country associated", bookingId);
                return;
            }

            var countryCode = booking.City.Country.Code;
            _logger.LogDebug("Booking country: {CountryCode}", countryCode);

            // Check if country requires real insurance
            var insuranceConfig = await _context.CountryInsuranceConfigs
                .FirstOrDefaultAsync(c => c.CountryCode == countryCode);

            if (insuranceConfig == null)
            {
                _logger.LogWarning("No insurance config found for country {CountryCode}, using default mock", countryCode);
            }

            bool requiresRealInsurance = insuranceConfig?.RequiresRealInsurance ?? false;
            _logger.LogDebug("Country {CountryCode} requires real insurance: {RequiresRealInsurance}", 
                countryCode, requiresRealInsurance);

            // Get appropriate provider
            var provider = _providerFactory.GetProvider(countryCode);

            // Create policy request
            var policyRequest = new PolicyRequest(
                BookingId: booking.Id,
                VehicleInfo: new VehicleInfo(
                    Make: booking.Vehicle.Make,
                    Model: booking.Vehicle.Model,
                    Year: booking.Vehicle.Year,
                    LicensePlate: booking.Vehicle.LicensePlate,
                    VIN: booking.Vehicle.VIN ?? "UNKNOWN"
                ),
                RenterInfo: new RenterInfo(
                    FirstName: booking.Renter.FirstName,
                    LastName: booking.Renter.LastName,
                    Email: booking.Renter.Email,
                    Phone: booking.Renter.PhoneNumber,
                    DateOfBirth: booking.Renter.DateOfBirth,
                    LicenseNumber: booking.Renter.LicenseNumber
                ),
                ProtectionPlanInfo: new ProtectionPlanInfo(
                    PlanName: booking.ProtectionPlan.Name,
                    PlanType: booking.ProtectionPlan.PlanType.ToString(),
                    Deductible: booking.ProtectionPlan.DeductibleAmount,
                    LiabilityCoverage: booking.ProtectionPlan.LiabilityCoverage
                ),
                StartDate: booking.StartDate,
                EndDate: booking.EndDate,
                TotalDays: (int)(booking.EndDate - booking.StartDate).TotalDays
            );

            // Create policy with provider
            _logger.LogDebug("Creating insurance policy with provider for booking {BookingId}", bookingId);
            var policyResponse = await provider.CreatePolicyAsync(policyRequest);

            if (policyResponse == null || !policyResponse.IsSuccess)
            {
                _logger.LogError("Failed to create insurance policy for booking {BookingId}: {Error}", 
                    bookingId, policyResponse?.ErrorMessage ?? "Unknown error");
                return;
            }

            // Store policy in database
            var insurancePolicy = new InsurancePolicy
            {
                BookingId = booking.Id,
                ProtectionPlanId = booking.ProtectionPlanId,
                PolicyNumber = policyResponse.PolicyNumber,
                InsuranceProviderName = policyResponse.ProviderName,
                CoverageStartDate = policyResponse.CoverageStartDate,
                CoverageEndDate = policyResponse.CoverageEndDate,
                PremiumAmount = policyResponse.PremiumAmount,
                LiabilityCoverage = policyResponse.LiabilityCoverage,
                Status = policyResponse.Status,
                ProviderPolicyJson = policyResponse.ProviderPolicyJson,
                IssuedAt = DateTime.UtcNow
            };

            _context.InsurancePolicies.Add(insurancePolicy);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Insurance policy created: {PolicyNumber} for booking {BookingId}", 
                policyResponse.PolicyNumber, bookingId);

            // Generate certificate PDF
            _logger.LogDebug("Generating insurance certificate for booking {BookingId}", bookingId);
            var certificateUrl = await _certificateGenerator.GenerateCertificateAsync(booking.Id);

            if (!string.IsNullOrEmpty(certificateUrl))
            {
                // Update policy and booking with certificate URL
                insurancePolicy.CertificateUrl = certificateUrl;
                booking.InsuranceCertificateUrl = certificateUrl;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Insurance certificate generated for booking {BookingId}: {CertificateUrl}", 
                    bookingId, certificateUrl);
            }
            else
            {
                _logger.LogWarning("Failed to generate certificate for booking {BookingId}", bookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing insurance for booking {BookingId}", bookingId);
            // Don't throw - we don't want insurance failures to block bookings
        }
    }

    public async Task<InsurancePolicy?> GetBookingPolicyAsync(int bookingId)
    {
        return await _context.InsurancePolicies
            .Include(p => p.Booking)
            .Include(p => p.ProtectionPlan)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
    }
}
