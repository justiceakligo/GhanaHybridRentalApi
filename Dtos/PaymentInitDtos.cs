namespace GhanaHybridRentalApi.Dtos;

public record InitializePaymentRequest(string Method, string? CustomerEmail = null, string? CustomerName = null);

public record InitializePaymentResponse(string Provider, string Reference, string? AuthorizationUrl, string? ClientSecret);
