using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class City
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty; // e.g., "Accra", "Kumasi"

    [MaxLength(128)]
    public string? Region { get; set; } // e.g., "Greater Accra", "Ashanti"

    [MaxLength(8)]
    public string? CountryCode { get; set; } = "GH"; // Ghana

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0; // For sorting in UI

    [MaxLength(512)]
    public string? Description { get; set; }

    public decimal? DefaultDeliveryFee { get; set; } // Optional delivery fee to this city

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Vehicle>? Vehicles { get; set; }
}
