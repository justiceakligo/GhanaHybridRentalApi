using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class CarCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    public decimal DefaultDailyRate { get; set; }

    public decimal MinDailyRate { get; set; }

    public decimal MaxDailyRate { get; set; }

    public decimal DefaultDepositAmount { get; set; }

    public bool RequiresDriver { get; set; } = false;

    // Soft-delete / active flag
    public bool IsActive { get; set; } = true;
}
