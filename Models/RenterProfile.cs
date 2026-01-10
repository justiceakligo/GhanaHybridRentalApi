using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GhanaHybridRentalApi.Models;

public class RenterProfile
{
    [Key]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [MaxLength(256)]
    public string? FullName { get; set; }

    [MaxLength(64)]
    public string? Nationality { get; set; }

    public DateTime? Dob { get; set; }

    [MaxLength(32)]
    public string VerificationStatus { get; set; } = "unverified"; // unverified, basic_verified, driver_verified

    public string? DocumentsJson { get; set; }

    // Driver's License (required for self-drive)
    [MaxLength(64)]
    public string? DriverLicenseNumber { get; set; }
    public DateTime? DriverLicenseExpiryDate { get; set; }
    [MaxLength(256)]
    public string? DriverLicensePhotoUrl { get; set; }

    // National ID (Ghana Card or Passport - required for bookings with driver)
    [MaxLength(64)]
    public string? NationalIdNumber { get; set; } // Ghana Card number
    [MaxLength(256)]
    public string? NationalIdPhotoUrl { get; set; }

    [MaxLength(64)]
    public string? PassportNumber { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    [MaxLength(256)]
    public string? PassportPhotoUrl { get; set; }

    // Address Information
    [MaxLength(512)]
    public string? StreetAddress { get; set; }
    [MaxLength(128)]
    public string? City { get; set; }

    // Emergency Contact
    [MaxLength(256)]
    public string? EmergencyContactName { get; set; }
    [MaxLength(32)]
    public string? EmergencyContactPhone { get; set; }

    public User? User { get; set; }
}
