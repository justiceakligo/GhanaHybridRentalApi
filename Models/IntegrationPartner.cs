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

    [MaxLength(64)]
    public string? ReferralCode { get; set; }

    [MaxLength(512)]
    public string? WebhookUrl { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }
}
