using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GhanaHybridRentalApi.Dtos;

namespace GhanaHybridRentalApi.Services;

public record PaystackInitializationResult(string Reference, string AuthorizationUrl);

public interface IPaystackPaymentService
{
    Task<PaystackInitializationResult> InitializeTransactionAsync(decimal amount, string email, string reference, Dictionary<string, string> metadata);
    Task<PaymentVerificationResult> VerifyTransactionAsync(string reference);
    Task<PaystackRefundResult> RefundTransactionAsync(string reference, decimal? amount = null);
    Task<PaystackCaptureResult> CaptureTransactionAsync(string reference, decimal amount, string currency);
}

public record PaystackRefundResult(bool Success, string? RefundId, string? Message);
public record PaystackCaptureResult(bool Success, string? Message);

public class PaystackPaymentService : IPaystackPaymentService
{
    private readonly IAppConfigService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaystackPaymentService> _logger;
    private readonly HttpClient _httpClient;

    public PaystackPaymentService(IAppConfigService configService, IConfiguration configuration, ILogger<PaystackPaymentService> logger, HttpClient httpClient)
    {
        _configService = configService;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    private async Task<string?> GetSecretKeyAsync()
    {
        var secretKey = await _configService.GetConfigValueAsync("Payment:Paystack:SecretKey");
        
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            secretKey = _configuration["Paystack:SecretKey"];
        }

        return secretKey;
    }

    private async Task<string> GetCurrencyAsync()
    {
        var currency = await _configService.GetConfigValueAsync("Payment:Currency");
        return currency ?? "GHS";
    }

    public async Task<PaystackInitializationResult> InitializeTransactionAsync(decimal amount, string email, string reference, Dictionary<string, string> metadata)
    {
        try
        {
            var secretKey = await GetSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey not configured");
            }

            // Trim whitespace and perform defensive validation
            secretKey = secretKey.Trim();

            // Detect common mis-typed prefixes like "sceret ", or accidental inclusion of labels
            if (secretKey.StartsWith("sceret", StringComparison.OrdinalIgnoreCase) || secretKey.StartsWith("secret", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Paystack secret key appears to contain an accidental prefix. Aborting initialization.");
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey appears to be malformed. Please ensure only the secret key (sk_...) is stored, without extra text or labels.");
            }

            // Defensive check: ensure the configured key looks like a secret (sk_...) and not a publishable key (pk_...)
            if (secretKey.StartsWith("pk_", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Paystack secret key appears to be a publishable key (starts with pk_). Aborting initialization.");
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey appears to be a publishable key (starts with 'pk_'). Please configure the secret key (sk_...).");
            }

            var currency = await GetCurrencyAsync();
            var url = "https://api.paystack.co/transaction/initialize";
            
            var payload = new
            {
                amount = (int)(amount * 100), // Convert to kobo (GHS minor unit)
                email = email,
                reference = reference,
                metadata = metadata,
                currency = currency
            };

            _httpClient.DefaultRequestHeaders.Clear();
            // Ensure Authorization header is properly formatted: "Bearer {secretKey}". Use AuthenticationHeaderValue to avoid formatting issues.
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

            // Log outgoing payload (without sensitive keys)
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            _logger.LogInformation("Sending Paystack initialize payload: {Payload}", payloadJson);

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Paystack API response: {StatusCode} - {Content}", response.StatusCode, content);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(content);
                _logger.LogInformation("Initialized Paystack transaction: {Reference}, URL: {AuthUrl}", reference, result?.Data?.AuthorizationUrl);
                return new PaystackInitializationResult(result?.Data?.Reference ?? reference, result?.Data?.AuthorizationUrl ?? string.Empty);
            }
            else
            {
                // Map some provider errors to clearer exceptions
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Paystack returned 401 Unauthorized - likely invalid secret key: {Content}", content);
                    throw new PaymentProviderNotConfiguredException("paystack", "Invalid Paystack SecretKey (received 401 Unauthorized). Please verify the configured secret key.");
                }

                _logger.LogError("Failed to initialize Paystack transaction: {Error}", content);
                // surface provider errors as service exceptions
                throw new Exception($"Paystack initialization failed: {content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Paystack transaction");
            throw;
        }
    }

    public async Task<PaymentVerificationResult> VerifyTransactionAsync(string reference)
    {
        try
        {
            var secretKey = await GetSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey not configured");
            }

            var url = $"https://api.paystack.co/transaction/verify/{reference}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(content);
                var status = result?.Data?.Status;
                
                if (status == "success")
                {
                    _logger.LogInformation("Verified Paystack transaction {Reference}: Success", reference);
                    return new PaymentVerificationResult { Success = true };
                }

                // Categorize Paystack failures
                var errorType = status switch
                {
                    "failed" => "declined",
                    "abandoned" => "declined",
                    "reversed" => "declined",
                    _ => "unknown"
                };

                var gatewayResponse = result?.Data?.GatewayResponse ?? "Transaction not successful";
                
                // Further categorize based on gateway response
                if (gatewayResponse.Contains("insufficient", StringComparison.OrdinalIgnoreCase))
                {
                    errorType = "insufficient_funds";
                }
                else if (gatewayResponse.Contains("declined", StringComparison.OrdinalIgnoreCase) || 
                         gatewayResponse.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    errorType = "declined";
                }
                else if (gatewayResponse.Contains("timeout", StringComparison.OrdinalIgnoreCase) || 
                         gatewayResponse.Contains("network", StringComparison.OrdinalIgnoreCase))
                {
                    errorType = "network";
                }
                else if (gatewayResponse.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                {
                    errorType = "invalid_details";
                }

                _logger.LogInformation("Paystack transaction {Reference} status: {Status}, Gateway: {Gateway}", 
                    reference, status, gatewayResponse);

                return new PaymentVerificationResult
                {
                    Success = false,
                    ErrorMessage = gatewayResponse,
                    ErrorType = errorType,
                    RawResponse = content
                };
            }
            else
            {
                _logger.LogError("Failed to verify Paystack transaction: {Error}", content);
                
                // Network or API error
                return new PaymentVerificationResult
                {
                    Success = false,
                    ErrorMessage = "Unable to verify payment with Paystack",
                    ErrorType = response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ? "network" : "provider_error",
                    RawResponse = content
                };
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error verifying Paystack transaction");
            return new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = "Network error connecting to payment provider",
                ErrorType = "network",
                RawResponse = httpEx.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Paystack transaction");
            return new PaymentVerificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorType = "provider_error",
                RawResponse = ex.ToString()
            };
        }
    }

    public async Task<PaystackRefundResult> RefundTransactionAsync(string reference, decimal? amount = null)
    {
        try
        {
            var secretKey = await GetSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey not configured");
            }

            var url = "https://api.paystack.co/refund";
            
            var payload = new
            {
                transaction = reference,
                amount = amount.HasValue ? (int)(amount.Value * 100) : (int?)null
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var doc = JsonSerializer.Deserialize<JsonElement>(content);
                    if (doc.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
                    {
                        var refundId = id.GetString();
                        _logger.LogInformation("Created Paystack refund for {Reference}, RefundId: {RefundId}", reference, refundId);
                        return new PaystackRefundResult(true, refundId, null);
                    }
                }
                catch { }

                _logger.LogInformation("Created Paystack refund for {Reference}", reference);
                return new PaystackRefundResult(true, null, null);
            }
            else
            {
                _logger.LogError("Failed to create Paystack refund: {Error}", content);
                return new PaystackRefundResult(false, null, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Paystack refund");
            return new PaystackRefundResult(false, null, ex.Message);
        }
    }

    public async Task<PaystackCaptureResult> CaptureTransactionAsync(string reference, decimal amount, string currency)
    {
        try
        {
            var secretKey = await GetSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new PaymentProviderNotConfiguredException("paystack", "Paystack SecretKey not configured");
            }

            var url = "https://api.paystack.co/transaction/capture";
            var payload = new
            {
                reference = reference,
                amount = (int)(amount * 100),
                currency = currency
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Captured Paystack transaction {Reference}", reference);
                return new PaystackCaptureResult(true, null);
            }
            else
            {
                _logger.LogError("Failed to capture Paystack transaction: {Error}", content);
                return new PaystackCaptureResult(false, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing Paystack transaction");
            return new PaystackCaptureResult(false, ex.Message);
        }
    }
}

// Removed FakePaystackPaymentService as requested. Use real Paystack service and configure secrets in AppConfig or environment.

// DTOs for Paystack responses
internal class PaystackInitializeResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public PaystackInitializeData? Data { get; set; }
}

internal class PaystackInitializeData
{
    [JsonPropertyName("authorization_url")]
    public string AuthorizationUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("access_code")]
    public string AccessCode { get; set; } = string.Empty;
    
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}

internal class PaystackVerifyResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public PaystackVerifyData? Data { get; set; }
}

internal class PaystackVerifyData
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("gateway_response")]
    public string? GatewayResponse { get; set; }
}
