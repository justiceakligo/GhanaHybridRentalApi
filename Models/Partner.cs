using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Partner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? LogoUrl { get; set; }

    [MaxLength(512)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [MaxLength(64)]
    public string City { get; set; } = string.Empty;

    [MaxLength(8)]
    public string Country { get; set; } = "GH";

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Comma-separated: "owner", "renter", or "owner,renter"
    [MaxLength(64)]
    public string TargetRoles { get; set; } = string.Empty;

    // Comma-separated categories
    // Owner: "gps_tracking", "mechanic", "car_wash", "insurance", "tyre_shop"
    // Renter: "hotel", "restaurant", "tour", "airport_transfer", "attraction"
    [MaxLength(256)]
    public string Categories { get; set; } = string.Empty;

    // For ranking (0-100)
    public int PriorityScore { get; set; } = 50;

    public bool IsFeatured { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [MaxLength(64)]
    public string? ReferralCode { get; set; }

    public string? Metadata { get; set; } // JSON for extra config

    // JSON-serialized arrays and objects for imageUrls, tags, business hours, contact, integration, and verification details
    public string? ImageUrlsJson { get; set; }
    public string? TagsJson { get; set; }
    public string? BusinessHoursJson { get; set; }
    public string? ContactJson { get; set; }
    public string? IntegrationJson { get; set; }
    public bool IsVerified { get; set; } = false;
    [MaxLength(128)]
    public string? VerificationBadge { get; set; }

    // Ratings are derived but stored here as cached fields
    public decimal? RatingAvg { get; set; }
    public int? RatingCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation to photos
    public List<PartnerPhoto>? Photos { get; set; }
}

public class PartnerClick
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PartnerId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BookingId { get; set; }

    [MaxLength(32)]
    public string? Role { get; set; } // renter, owner

    [MaxLength(64)]
    public string? City { get; set; }

    [MaxLength(32)]
    public string EventType { get; set; } = "click"; // click, view, conversion

    public decimal? ConversionAmount { get; set; }

    [MaxLength(128)]
    public string? ExternalReference { get; set; }

    public DateTime CreatedAt { get; set; }

    public Partner? Partner { get; set; }
    public User? User { get; set; }
    public Booking? Booking { get; set; }
}
