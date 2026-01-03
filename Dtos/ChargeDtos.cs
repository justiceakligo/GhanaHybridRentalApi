namespace GhanaHybridRentalApi.Dtos;

public record ChargeTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    decimal DefaultAmount,
    string Currency,
    string RecipientType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateChargeTypeRequest(
    string Code,
    string Name,
    string? Description,
    decimal DefaultAmount,
    string Currency,
    string RecipientType
);

public record UpdateChargeTypeRequest(
    string? Name,
    string? Description,
    decimal? DefaultAmount,
    string? Currency,
    string? RecipientType,
    bool? IsActive
);

public record BookingChargeResponse(
    Guid Id,
    Guid BookingId,
    Guid ChargeTypeId,
    string ChargeTypeCode,
    string ChargeTypeName,
    decimal Amount,
    string Currency,
    string? Label,
    string? Notes,
    IReadOnlyList<string> EvidencePhotoUrls,
    string Status,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    string? CreatedByName,
    DateTime? SettledAt,
    Guid? PaymentTransactionId
);

// Owner cannot set amount; only admin.
public record CreateBookingChargeRequest(
    Guid ChargeTypeId,
    string? Label,
    string? Notes,
    List<string> EvidencePhotoUrls // REQUIRED
);

public record UpdateBookingChargeStatusRequest(
    string Status,
    Guid? PaymentTransactionId
);
