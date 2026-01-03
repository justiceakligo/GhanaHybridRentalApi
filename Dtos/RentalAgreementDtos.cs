namespace GhanaHybridRentalApi.Dtos;

public record RentalAgreementTemplateDto(
    Guid Id,
    string Code,
    string Version,
    string Title,
    string BodyText,
    bool RequireNoSmokingConfirmation,
    bool RequireFinesAndTicketsConfirmation,
    bool RequireAccidentProcedureConfirmation
);

public record UpdateRentalAgreementTemplateRequest(
    string? Code,
    string? Version,
    string? Title,
    string BodyText,
    bool RequireNoSmokingConfirmation,
    bool RequireFinesAndTicketsConfirmation,
    bool RequireAccidentProcedureConfirmation
);

public record BookingRentalAgreementView(
    Guid BookingId,
    string BookingReference,
    string TemplateCode,
    string TemplateVersion,
    string Title,
    string BodyText,
    bool RequireNoSmokingConfirmation,
    bool RequireFinesAndTicketsConfirmation,
    bool RequireAccidentProcedureConfirmation,
    bool AlreadyAccepted
);

public record AcceptRentalAgreementRequest(
    bool AcceptedNoSmoking,
    bool AcceptedFinesAndTickets,
    bool AcceptedAccidentProcedure,
    string? CustomerEmail = null,
    string? CustomerName = null
);

public record RentalAgreementAcceptanceDto(
    Guid BookingId,
    Guid RenterId,
    string TemplateCode,
    string TemplateVersion,
    bool AcceptedNoSmoking,
    bool AcceptedFinesAndTickets,
    bool AcceptedAccidentProcedure,
    DateTime AcceptedAt,
    string? IpAddress = null
);
