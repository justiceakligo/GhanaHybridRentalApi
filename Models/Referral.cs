using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string ReferralCode { get; set; } = string.Empty;

    public Guid? ReferrerUserId { get; set; }

    public Guid? ReferredUserId { get; set; }

    public Guid? IntegrationPartnerId { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "pending"; // pending, completed, rewarded

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public decimal? RewardAmount { get; set; }

    [MaxLength(8)]
    public string? RewardCurrency { get; set; }

    public User? ReferrerUser { get; set; }
    public User? ReferredUser { get; set; }
    public IntegrationPartner? IntegrationPartner { get; set; }
}
