using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    [MaxLength(32)]
    public string PlateNumber { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Make { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? CityId { get; set; } // Foreign key to City

    [MaxLength(16)]
    public string Transmission { get; set; } = "automatic"; // automatic | manual

    [MaxLength(32)]
    public string FuelType { get; set; } = "petrol";

    public int SeatingCapacity { get; set; } = 5;

    public bool HasAC { get; set; } = true;

    public DateTime? AvailableFrom { get; set; }

    public DateTime? AvailableUntil { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "pending_review"; // pending_review, active, inactive, suspended

    public string? PhotosJson { get; set; }
    // Optional explicit document URLs (preferred for admin validation)
    public string? InsuranceDocumentUrl { get; set; }
    public string? RoadworthinessDocumentUrl { get; set; }

    // Mileage charging fields
    public int IncludedKilometers { get; set; } = 0;
    public decimal PricePerExtraKm { get; set; } = 0.00m;
    public bool MileageChargingEnabled { get; set; } = true;

    // Auto-population fields for features, specifications, and inclusions
    public string? FeaturesJson { get; set; } // JSON array: ["Air Conditioning", "Bluetooth", ...]
    public string? SpecificationsJson { get; set; } // JSON object: {engineSize: "1.5L", fuelEfficiency: "15-17 km/L", ...}
    public string? InclusionsJson { get; set; } // JSON object: rental inclusions/policies

    // Override fields for mileage allowance (NULL uses global defaults)
    public int? MileageAllowancePerDay { get; set; } // NULL = use global default
    public decimal? ExtraKmRate { get; set; } // NULL = use global default

    // Additional vehicle specification fields (extracted for search/filter)
    public string? TransmissionType { get; set; } // Manual, Automatic, CVT

    // Optional per-vehicle daily rate (overrides category default if set)
    public decimal? DailyRate { get; set; }

    // Soft delete timestamp
    public DateTime? DeletedAt { get; set; }

    public User? Owner { get; set; }
    public CarCategory? Category { get; set; }
    public City? City { get; set; }
}
