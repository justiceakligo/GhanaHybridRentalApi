namespace GhanaHybridRentalApi.Dtos;

/// <summary>
/// Enhanced payment failure response to help frontend provide smart UX
/// </summary>
public class PaymentFailureResponse
{
    /// <summary>
    /// Generic error message for display
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// Detailed technical error message (for debugging)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Payment reference for tracking
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Categorized error type: network, declined, invalid_details, provider_error, insufficient_funds, unknown
    /// </summary>
    public required string ErrorType { get; set; }

    /// <summary>
    /// Whether the user can retry with the same payment method
    /// </summary>
    public bool CanRetry { get; set; }

    /// <summary>
    /// Whether suggesting an alternative payment method would be helpful
    /// </summary>
    public bool SuggestAlternative { get; set; }

    /// <summary>
    /// User-friendly message explaining what went wrong
    /// </summary>
    public required string UserMessage { get; set; }

    /// <summary>
    /// Recommended action for the user
    /// </summary>
    public required string RecommendedAction { get; set; }

    /// <summary>
    /// Alternative payment method to suggest (stripe for paystack failures, paystack for stripe failures)
    /// </summary>
    public string? AlternativeMethod { get; set; }

    /// <summary>
    /// The payment method that failed (card/mobile_money)
    /// </summary>
    public string? FailedMethod { get; set; }
}

/// <summary>
/// Result from payment provider verification with categorized error information
/// </summary>
public class PaymentVerificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ErrorType { get; set; } = "unknown";
    public string? RawResponse { get; set; }
}
