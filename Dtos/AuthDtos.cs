using System.Text.Json.Serialization;

namespace GhanaHybridRentalApi.Dtos;

public record RegisterRequest(
    string? Email, 
    string? Phone, 
    string Password, 
    string Role,
    string? FirstName = null,
    string? LastName = null,
    string? CompanyName = null,
    string? ConfirmPassword = null,
    string? TurnstileToken = null
);

public record LoginRequest(string? EmailOrPhone, string? Identifier, string Password, string? TurnstileToken = null)
{
    // Support both field names for backwards compatibility
    public string GetIdentifier() => EmailOrPhone ?? Identifier ?? string.Empty;
};

public record RequestOtpRequest(string Phone, string Purpose, string? Channel);
public record VerifyOtpRequest(string Phone, string Code, string? FullName, string? Role);

// Simplified phone authentication DTOs
public record SendPhoneCodeRequest(string Phone);

public record VerifyPhoneCodeRequest(
    string Phone, 
    string Code, 
    string? FirstName = null, 
    string? LastName = null, 
    string? Role = null
);

public record AuthResponse(Guid UserId, string Role, string Token, UserProfile? Profile = null);

public record UserProfile(
    Guid Id,
    string? Email,
    string? Phone,
    string? FirstName,
    string? LastName,
    string Role,
    string Status,
    bool PhoneVerified
);

public record SetPasswordRequest(string Phone, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string OldPassword, string NewPassword);

// Turnstile DTOs
public sealed class TurnstileVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("challenge_ts")]
    public string? ChallengeTs { get; set; }
}
