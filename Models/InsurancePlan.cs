using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class InsurancePlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public decimal DailyPrice { get; set; }

    [MaxLength(1024)]
    public string? CoverageSummary { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsDefault { get; set; }

    public bool Active { get; set; } = true;
}
