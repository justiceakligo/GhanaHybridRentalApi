using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class RentalAgreementAcceptance
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid RenterId { get; set; }
    public User Renter { get; set; } = null!;

    [MaxLength(20)]
    public string TemplateVersion { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TemplateCode { get; set; } = "default";

    public bool AcceptedNoSmoking { get; set; }
    public bool AcceptedFinesAndTickets { get; set; }
    public bool AcceptedAccidentProcedure { get; set; }

    /// <summary>
    /// Optional full snapshot of the agreement text at time of acceptance.
    /// </summary>
    public string? AgreementSnapshot { get; set; }

    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(200)]
    public string? UserAgent { get; set; }
}
