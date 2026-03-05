namespace GhanaHybridRentalApi.Services.Insurance;

/// <summary>
/// Request to create an insurance policy
/// </summary>
public record PolicyRequest
{
    public Guid BookingId { get; init; }
    public VehicleInfo Vehicle { get; init; } = null!;
    public RenterInfo Renter { get; init; } = null!;
    public DateTime CoverageStart { get; init; }
    public DateTime CoverageEnd { get; init; }
    public ProtectionPlanInfo ProtectionPlan { get; init; } = null!;
}

public record VehicleInfo
{
    public string VIN { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string LicensePlate { get; init; } = string.Empty;
}

public record RenterInfo
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
}

public record ProtectionPlanInfo
{
    public string Name { get; init; } = string.Empty;
    public decimal DailyPrice { get; init; }
    public decimal Deductible { get; init; }
}

/// <summary>
/// Response from insurance provider
/// </summary>
public record PolicyResponse
{
    public string PolicyNumber { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal PremiumAmount { get; init; }
    public decimal LiabilityCoverage { get; init; }
    public string Status { get; init; } = "active";
    public string? CertificateUrl { get; init; }
    public Dictionary<string, object>? AdditionalDetails { get; init; }
}

/// <summary>
/// Certificate data for PDF generation
/// </summary>
public record CertificateData
{
    public string BookingReference { get; init; } = string.Empty;
    public string RenterName { get; init; } = string.Empty;
    public string VehicleDetails { get; init; } = string.Empty;
    public string CoverageType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string PolicyNumber { get; init; } = string.Empty;
    public string InsuranceProvider { get; init; } = string.Empty;
    public decimal LiabilityCoverage { get; init; }
    public decimal CollisionDeductible { get; init; }
    public bool TheftCoverage { get; init; }
    public bool RoadsideAssistance { get; init; }
    public string? VehicleImage { get; init; }
    public string? QRCodeData { get; init; }
}
