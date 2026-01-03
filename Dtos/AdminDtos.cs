namespace GhanaHybridRentalApi.Dtos;

// Admin DTOs
public record UpdateUserStatusRequest(string Status); // active, suspended

public record UpdateVehicleStatusRequest(string Status, string? Notes); // active, inactive, suspended, rejected

public record RequestVehicleInfoRequest(string Notes);

public record VerifyDocumentRequest(bool? Approve); // true = verify/approve, false = reject

public record RenterActionRequest(string? Reason); // For suspend, reject actions

// Driver DTOs
public record CreateDriverProfileRequest(
    string FullName,
    string LicenseNumber,
    DateTime LicenseExpiryDate,
    string DriverType, // independent, owner_employed, platform
    Guid? OwnerEmployerId
);

public record UpdateDriverProfileRequest(
    string? FullName,
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    bool? Available
);

public record UpdateDriverVerificationRequest(string VerificationStatus); // verified, rejected

public record AssignDriverRequest(Guid DriverId);

// Integration Partner DTOs
public record CreateIntegrationPartnerRequest(
    string Name,
    string Type, // hotel, travel_agency, ota, custom
    string? ReferralCode,
    string? WebhookUrl
);

public record UpdateIntegrationPartnerRequest(
    string? Name,
    string? WebhookUrl,
    bool? Active
);

public record IntegrationPartnerResponse(
    Guid Id,
    string Name,
    string Type,
    string ApiKey,
    string? ReferralCode,
    string? WebhookUrl,
    bool Active,
    DateTime CreatedAt,
    DateTime? LastUsedAt
);

// Payment DTOs
public record CreatePaymentTransactionRequest(
    Guid? BookingId,
    string Type, // payment, refund, payout, deposit
    decimal Amount,
    string Method, // momo, card, bank
    string? Reference,
    Dictionary<string, object>? Metadata,
    string? CustomerEmail = null,
    string? CustomerName = null
);

public record UpdatePaymentTransactionRequest(
    string Status, // completed, failed, cancelled
    string? ExternalTransactionId,
    string? ErrorMessage
);

public record PaymentTransactionResponse(
    Guid Id,
    Guid? BookingId,
    Guid UserId,
    string Type,
    string Status,
    decimal Amount,
    string Currency,
    string Method,
    string? ExternalTransactionId,
    string? Reference,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record PaymentTransactionDetailedResponse(
    Guid Id,
    Guid? BookingId,
    Guid UserId,
    string Type,
    string Status,
    decimal Amount,
    string Currency,
    string Method,
    string? ExternalTransactionId,
    string? Reference,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    decimal? CapturedAmount,
    string? BookingReference,
    string? RenterName,
    string? RenterEmail,
    string? VehicleName
);

// Payout DTOs
public record CreatePayoutRequest(
    Guid OwnerId,
    decimal Amount,
    string Method, // momo, bank
    DateTime PeriodStart,
    DateTime PeriodEnd,
    List<Guid> BookingIds
);

public record UpdatePayoutStatusRequest(
    string Status, // processing, completed, failed
    string? ExternalPayoutId,
    string? ErrorMessage
);

public record PayoutResponse(
    Guid Id,
    Guid OwnerId,
    decimal Amount,
    string Currency,
    string Status,
    string Method,
    string? Reference,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int BookingCount
);

// Referral DTOs
public record CreateReferralRequest(
    string ReferralCode,
    Guid? IntegrationPartnerId
);

public record ReferralResponse(
    Guid Id,
    string ReferralCode,
    Guid? ReferrerUserId,
    Guid? ReferredUserId,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    decimal? RewardAmount
);

// Admin DTOs
public record TestEmailRequest(string? ToEmail, string? EmailOrPhone);

public record UpdateOwnerRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Status,
    string? DisplayName,
    string? CompanyName
);
