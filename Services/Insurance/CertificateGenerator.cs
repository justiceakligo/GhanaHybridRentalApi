using GhanaHybridRentalApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GhanaHybridRentalApi.Services.Insurance;

/// <summary>
/// Generates insurance certificate PDFs for bookings
/// Currently generates HTML certificates - TODO: Replace with proper PDF generation
/// </summary>
public class CertificateGenerator : ICertificateGenerator
{
    private readonly AppDbContext _context;
    private readonly ILogger<CertificateGenerator> _logger;
    private readonly IConfiguration _configuration;

    public CertificateGenerator(
        AppDbContext context,
        ILogger<CertificateGenerator> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string?> GenerateCertificateAsync(int bookingId)
    {
        try
        {
            _logger.LogInformation("Generating certificate for booking {BookingId}", bookingId);

            // Load all booking data
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
                return null;
            }

            // Load insurance policy
            var policy = await _context.InsurancePolicies
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (policy == null)
            {
                _logger.LogError("No insurance policy found for booking {BookingId}", bookingId);
                return null;
            }

            // Create certificate data
            var certificateData = new CertificateData(
                BookingId: booking.Id,
                BookingReference: booking.BookingReference,
                PolicyNumber: policy.PolicyNumber,
                InsuranceProvider: policy.InsuranceProviderName,
                RenterName: $"{booking.Renter.FirstName} {booking.Renter.LastName}",
                RenterEmail: booking.Renter.Email,
                VehicleDetails: $"{booking.Vehicle.Year} {booking.Vehicle.Make} {booking.Vehicle.Model}",
                LicensePlate: booking.Vehicle.LicensePlate,
                ProtectionPlanName: booking.ProtectionPlan.Name,
                CoverageStartDate: policy.CoverageStartDate,
                CoverageEndDate: policy.CoverageEndDate,
                LiabilityCoverage: policy.LiabilityCoverage,
                Deductible: booking.ProtectionPlan.DeductibleAmount,
                CountryCode: booking.City.Country.Code,
                CountryName: booking.City.Country.Name,
                IssuedAt: DateTime.UtcNow
            );

            // Generate certificate (currently HTML, TODO: convert to PDF)
            var certificateHtml = GenerateCertificateHtml(certificateData);

            // Save certificate to file system or blob storage
            var certificateUrl = await SaveCertificateAsync(bookingId, certificateHtml);

            _logger.LogInformation("Certificate generated successfully for booking {BookingId}: {Url}", 
                bookingId, certificateUrl);

            return certificateUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate for booking {BookingId}", bookingId);
            return null;
        }
    }

    private string GenerateCertificateHtml(CertificateData data)
    {
        // TODO: Replace with proper PDF generation using QuestPDF, iTextSharp, or similar
        // This is a temporary HTML-based certificate for development

        var html = new StringBuilder();
        html.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Insurance Certificate - {data.BookingReference}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 800px;
            margin: 40px auto;
            padding: 40px;
            border: 2px solid #333;
            background-color: #fff;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #2c5aa0;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .title {{
            font-size: 28px;
            font-weight: bold;
            color: #2c5aa0;
            margin-bottom: 10px;
        }}
        .subtitle {{
            font-size: 16px;
            color: #666;
        }}
        .section {{
            margin-bottom: 25px;
        }}
        .section-title {{
            font-size: 18px;
            font-weight: bold;
            color: #2c5aa0;
            margin-bottom: 10px;
            border-bottom: 1px solid #ddd;
            padding-bottom: 5px;
        }}
        .info-row {{
            display: flex;
            padding: 8px 0;
            border-bottom: 1px solid #f0f0f0;
        }}
        .info-label {{
            font-weight: bold;
            width: 200px;
            color: #555;
        }}
        .info-value {{
            flex: 1;
            color: #333;
        }}
        .footer {{
            text-align: center;
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #2c5aa0;
            font-size: 12px;
            color: #666;
        }}
        .policy-number {{
            background-color: #f8f9fa;
            padding: 15px;
            border-left: 4px solid #2c5aa0;
            margin: 20px 0;
            font-size: 18px;
            font-weight: bold;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>CERTIFICATE OF INSURANCE</div>
        <div class='subtitle'>Rental Vehicle Protection Coverage</div>
    </div>

    <div class='policy-number'>
        Policy Number: {data.PolicyNumber}
    </div>

    <div class='section'>
        <div class='section-title'>Booking Information</div>
        <div class='info-row'>
            <div class='info-label'>Booking Reference:</div>
            <div class='info-value'>{data.BookingReference}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Insurance Provider:</div>
            <div class='info-value'>{data.InsuranceProvider}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Country:</div>
            <div class='info-value'>{data.CountryName}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>Insured Party</div>
        <div class='info-row'>
            <div class='info-label'>Name:</div>
            <div class='info-value'>{data.RenterName}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Email:</div>
            <div class='info-value'>{data.RenterEmail}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>Vehicle Information</div>
        <div class='info-row'>
            <div class='info-label'>Vehicle:</div>
            <div class='info-value'>{data.VehicleDetails}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>License Plate:</div>
            <div class='info-value'>{data.LicensePlate}</div>
        </div>
    </div>

    <div class='section'>
        <div class='section-title'>Coverage Details</div>
        <div class='info-row'>
            <div class='info-label'>Protection Plan:</div>
            <div class='info-value'>{data.ProtectionPlanName}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Coverage Start:</div>
            <div class='info-value'>{data.CoverageStartDate:MMMM dd, yyyy}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Coverage End:</div>
            <div class='info-value'>{data.CoverageEndDate:MMMM dd, yyyy}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Liability Coverage:</div>
            <div class='info-value'>${data.LiabilityCoverage:N2}</div>
        </div>
        <div class='info-row'>
            <div class='info-label'>Deductible:</div>
            <div class='info-value'>${data.Deductible:N2}</div>
        </div>
    </div>

    <div class='footer'>
        <p>This certificate is issued on {data.IssuedAt:MMMM dd, yyyy} at {data.IssuedAt:HH:mm:ss UTC}</p>
        <p>This is a valid certificate of insurance for the period shown above.</p>
        <p>For questions or claims, please contact {data.InsuranceProvider}.</p>
    </div>
</body>
</html>");

        return html.ToString();
    }

    private async Task<string> SaveCertificateAsync(int bookingId, string certificateHtml)
    {
        // TODO: In production, save to Azure Blob Storage, AWS S3, or similar
        // For now, save to local wwwroot/certificates folder

        var certificatesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates");
        Directory.CreateDirectory(certificatesPath);

        var fileName = $"certificate-{bookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}.html";
        var filePath = Path.Combine(certificatesPath, fileName);

        await File.WriteAllTextAsync(filePath, certificateHtml);

        // Return URL (this assumes the app serves static files from wwwroot)
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        return $"{baseUrl}/certificates/{fileName}";
    }
}
