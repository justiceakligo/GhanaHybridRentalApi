using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class RentalAgreementTemplate
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Code { get; set; } = "default"; // e.g., "ghana_standard"

    [MaxLength(20)]
    public string Version { get; set; } = "1.0.0";

    [MaxLength(200)]
    public string Title { get; set; } = "Ryve Rental Agreement";

    /// <summary>
    /// Full legal text shown to the renter.
    /// </summary>
    public string BodyText { get; set; } = string.Empty;

    /// <summary>
    /// Whether renter must explicitly tick a No Smoking clause.
    /// </summary>
    public bool RequireNoSmokingConfirmation { get; set; } = true;

    /// <summary>
    /// Whether renter must explicitly tick a Fines/Tickets responsibility clause.
    /// </summary>
    public bool RequireFinesAndTicketsConfirmation { get; set; } = true;

    /// <summary>
    /// Whether renter must explicitly tick an Accidents/Procedure clause.
    /// </summary>
    public bool RequireAccidentProcedureConfirmation { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
