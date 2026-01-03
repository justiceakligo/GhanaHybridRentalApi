using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(128)]
    public string? FirstName { get; set; }

    [MaxLength(128)]
    public string? LastName { get; set; }

    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Role { get; set; } = "renter"; // admin, owner, renter, driver

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, active, suspended

    public bool PhoneVerified { get; set; } = false;

    [MaxLength(256)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public string? NotificationPreferencesJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public OwnerProfile? OwnerProfile { get; set; }
    public RenterProfile? RenterProfile { get; set; }
    public DriverProfile? DriverProfile { get; set; }
}
