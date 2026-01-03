using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Airport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty; // e.g., "Kotoka International Airport"

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty; // e.g., "ACC" (IATA code)

    public Guid CityId { get; set; } // Which city this airport belongs to

    [MaxLength(512)]
    public string? Address { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public decimal? PickupFee { get; set; } // Additional fee for airport pickup
    public decimal? DropoffFee { get; set; } // Additional fee for airport dropoff

    public int DisplayOrder { get; set; } = 0;

    [MaxLength(1024)]
    public string? Instructions { get; set; } // Special instructions for airport pickup/dropoff

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public City? City { get; set; }
}
