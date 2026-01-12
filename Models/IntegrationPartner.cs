using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class IntegrationPartner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Type { get; set; } = "custom"; // hotel, travel_agency, ota, custom

    [MaxLength(256)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API key expiry date (null = no expiry)
    /// </summary>
    public DateTime? ApiKeyExpiresAt { get; set; }

    [MaxLength(64)]
    public string? ReferralCode { get; set; }

    [MaxLength(512)]
    public string? WebhookUrl { get; set; }

    public bool Active { get; set; } = true;

    // Application/Contact fields
    [MaxLength(512)]
    public string? ContactPerson { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(512)]
    public string? Website { get; set; }

    [MaxLength(128)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Application reference number (e.g., PA-2026-001234)
    /// </summary>
    [MaxLength(64)]
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Admin notes about partner application/account
    /// </summary>
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Partner's commission percentage (default 15%)
    /// </summary>
    public decimal CommissionPercent { get; set; } = 15.00m;

    /// <summary>
    /// Payment terms in days (default 30 days)
    /// </summary>
    public int SettlementTermDays { get; set; } = 30;

    /// <summary>
    /// Whether partner bookings are auto-confirmed or require admin approval
    /// </summary>
    public bool AutoConfirmBookings { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}
