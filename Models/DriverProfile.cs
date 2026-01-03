using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhanaHybridRentalApi.Models;

public class DriverProfile
{
    [Key]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [MaxLength(256)]
    public string? FullName { get; set; }

    [MaxLength(64)]
    public string? LicenseNumber { get; set; }

    public DateTime? LicenseExpiryDate { get; set; }

    [MaxLength(32)]
    public string VerificationStatus { get; set; } = "unverified"; // unverified, pending, verified, rejected

    public Guid? OwnerEmployerId { get; set; }

    [MaxLength(32)]
    public string DriverType { get; set; } = "independent"; // independent, owner_employed, platform

    public bool Available { get; set; } = true;

    [MaxLength(512)]
    public string? PhotoUrl { get; set; } // Driver profile photo URL

    [MaxLength(1000)]
    public string? Bio { get; set; } // Driver biography/description

    public decimal? DailyRate { get; set; } // Driver daily rate

    public int? YearsOfExperience { get; set; } // Years of driving experience

    public decimal? AverageRating { get; set; } // Calculated from reviews

    public int? TotalTrips { get; set; } // Number of completed trips

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? DocumentsJson { get; set; }

    public User? User { get; set; }
    public User? OwnerEmployer { get; set; }
}
