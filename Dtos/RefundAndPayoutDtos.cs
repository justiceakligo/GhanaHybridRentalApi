using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GhanaHybridRentalApi.Dtos;

// Payout Details DTOs
public record PayoutDetailsDto(
    string? AccountNumber,
    string? AccountName,
    string? BankName,
    string? Provider,  // For MoMo: MTN, Vodafone, AirtelTigo
    string? BankCode,
    string? BranchCode,
    string? SwiftCode
)
{
    public static PayoutDetailsDto? Parse(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            if (dict == null) return null;

            // Normalize legacy keys for backwards compatibility
            var accountNumber = dict.GetValueOrDefault("accountNumber") ?? dict.GetValueOrDefault("momoNumber");
            var provider = dict.GetValueOrDefault("provider") ?? dict.GetValueOrDefault("momoProvider");

            return new PayoutDetailsDto(
                accountNumber,
                dict.GetValueOrDefault("accountName"),
                dict.GetValueOrDefault("bankName"),
                provider,
                dict.GetValueOrDefault("bankCode"),
                dict.GetValueOrDefault("branchCode"),
                dict.GetValueOrDefault("swiftCode")
            );
        }
        catch
        {
            return null;
        }
    }
}

// Deposit Refund DTOs
public record DepositRefundResponse(
    Guid Id,
    Guid BookingId,
    string BookingReference,
    decimal Amount,
    string Currency,
    string Status,
    string PaymentMethod,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? ProcessedAt,
    DateTime? CompletedAt,
    bool IsOverdue,
    int DaysOverdue,
    string? ErrorMessage,
    string? Notes,
    // Booking details
    string? RenterName,
    string? RenterEmail,
    string? VehicleInfo
);

public record ProcessRefundRequest(
    [Required] string Notes
);

public record CreateDepositRefundRequest(
    [Required] Guid BookingId,
    string? Notes
);

// Instant Withdrawal DTOs
public record InstantWithdrawalRequest(
    [Required, Range(1, 1000000)] decimal Amount
);

public record InstantWithdrawalResponse(
    Guid Id,
    decimal Amount,
    decimal FeeAmount,
    decimal FeePercentage,
    decimal NetAmount,
    string Currency,
    string Status,
    string Method,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    PayoutDetailsDto? PayoutDetails = null
);

// Payout Settings DTOs
public record UpdatePayoutSettingsRequest(
    [Required] string PayoutFrequency, // daily, weekly, biweekly, monthly
    [Required, Range(0, 10000)] decimal MinimumPayoutAmount,
    bool InstantWithdrawalEnabled
);

public record PayoutSettingsResponse(
    string PayoutFrequency,
    decimal MinimumPayoutAmount,
    bool InstantWithdrawalEnabled,
    decimal InstantWithdrawalFeePercentage
);

public record ScheduledPayoutResponse(
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    decimal AvailableBalance,
    decimal MinimumPayoutAmount,
    string PayoutFrequency,
    DateTime LastPayoutDate,
    DateTime NextPayoutDate,
    bool IsDueToday,
    string PayoutMethod,
    PayoutDetailsDto? PayoutDetails,
    PayoutDetailsDto? PayoutDetailsPending = null,
    string? PayoutVerificationStatus = null
);

public record ProcessScheduledPayoutsRequest(
    [Required] List<Guid> OwnerIds
);

// Payout Verification DTOs
public record VerifyPayoutMethodRequest(
    bool Approve,
    string? Reason
);

public record UpdatePayoutDetailsRequest(
    [Required] string PayoutPreference, // momo or bank
    [Required] Dictionary<string, string> PayoutDetails
);

public record CapturePaymentRequest(
    [Required] decimal CaptureAmount,
    [Required] string Currency,
    string? Reason
);
