using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class ProtectionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    // Pricing
    [MaxLength(32)]
    public string PricingMode { get; set; } = "per_day"; // per_day | fixed

    public decimal DailyPrice { get; set; }
    public decimal FixedPrice { get; set; }

    public decimal MinFee { get; set; }
    public decimal MaxFee { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "GHS";

    // Minor Damage Waiver
    public bool IncludesMinorDamageWaiver { get; set; }
    public decimal? MinorWaiverCap { get; set; }
    public decimal? Deductible { get; set; }

    // Exclusions stored as JSON array of strings (theft, total_loss...)
    public string? ExcludesJson { get; set; }

    public bool IsMandatory { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
