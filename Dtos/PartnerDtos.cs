namespace GhanaHybridRentalApi.Dtos;

public record ContactInfo(
    string? Email,
    string? Whatsapp,
    string? Other
);

public record BusinessHour(
    string Day,
    string Open,
    string Close,
    bool Closed
);

public record CreatePartnerRequest(
    string Name,
    string Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? PhoneNumber,
    string City,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    List<string> TargetRoles,
    List<string> Categories,
    int PriorityScore,
    bool IsFeatured,
    bool IsActive,
    string? ReferralCode,
    string? Metadata,
    List<string>? ImageUrls,
    List<string>? Tags,
    ContactInfo? Contact,
    List<BusinessHour>? BusinessHours,
    bool? IsVerified,
    string? VerificationBadge
);

public record UpdatePartnerRequest(
    string? Name,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? PhoneNumber,
    string? City,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    List<string>? TargetRoles,
    List<string>? Categories,
    int? PriorityScore,
    bool? IsFeatured,
    bool? IsActive,
    string? ReferralCode,
    string? Metadata,
    List<string>? ImageUrls,
    List<string>? Tags,
    ContactInfo? Contact,
    List<BusinessHour>? BusinessHours,
    bool? IsVerified,
    string? VerificationBadge
);

public record PartnerClickRequest(
    string Role,
    string? City,
    Guid? BookingId
);

public record PartnerConversionRequest(
    string ExternalReference,
    Guid? BookingId,
    decimal Amount,
    string Currency
);

public record PartnerSuggestionResponse(
    Guid Id,
    string Name,
    string Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? PhoneNumber,
    string City,
    string Country,
    List<string> Categories,
    List<string> TargetRoles,
    int PriorityScore,
    bool IsFeatured,
    string? ReferralCode,
    List<string>? ImageUrls,
    List<string>? Tags
);

public record AdminPartnerResponse(
    Guid Id,
    string Name,
    string Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? PhoneNumber,
    List<string>? Images,
    List<string>? Tags,
    ContactInfo? Contact,
    string City,
    string Country,
    List<string> TargetRoles,
    List<string> Categories,
    int PriorityScore,
    bool IsFeatured,
    bool IsActive,
    string? ReferralCode,
    string? Metadata,
    bool IsVerified,
    string? VerificationBadge,
    decimal? RatingAvg,
    int? RatingCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
