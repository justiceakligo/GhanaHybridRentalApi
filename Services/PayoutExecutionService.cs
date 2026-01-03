using System.Text.Json;

namespace GhanaHybridRentalApi.Services;

public interface IPayoutExecutionService
{
    Task<bool> ExecutePayoutAsync(Guid payoutId, string paymentMethod, Dictionary<string, string> paymentDetails);
}

public class PayoutExecutionService : IPayoutExecutionService
{
    private readonly ILogger<PayoutExecutionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IAppConfigService _configService;

    public PayoutExecutionService(
        ILogger<PayoutExecutionService> logger,
        HttpClient httpClient,
        IConfiguration configuration,
        IAppConfigService configService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
        _configService = configService;
    }

    private async Task<string?> GetPaystackSecretKeyAsync()
    {
        // Get from database config (admin-configured)
        var key = await _configService.GetConfigValueAsync("Payment:Paystack:SecretKey");
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        // Fallback to appsettings.json
        return _configuration["Paystack:SecretKey"];
    }

    private async Task<string?> CreatePaystackRecipientAsync(
        string ownerEmail,
        string accountNumber,
        string bankCode,
        string accountName,
        string type = "nuban")
    {
        try
        {
            var secretKey = await GetPaystackSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("Paystack secret key not configured");
                return null;
            }

            var payload = new
            {
                type = type,
                name = accountName,
                account_number = accountNumber,
                bank_code = bankCode,
                currency = "GHS",
                email = ownerEmail
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.paystack.co/transferrecipient",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Paystack recipient: {Error}", error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data) && 
                data.TryGetProperty("recipient_code", out var code))
            {
                _logger.LogInformation("Created Paystack recipient: {Code}", code.GetString());
                return code.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Paystack recipient");
            return null;
        }
    }

    private async Task<(bool Success, string? TransferCode, string? Status)> InitiatePaystackTransferAsync(
        string recipientCode,
        decimal amount,
        string reference,
        string reason)
    {
        try
        {
            var secretKey = await GetPaystackSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return (false, null, "Paystack secret key not configured");
            }

            var amountInPesewas = (int)(amount * 100);

            var payload = new
            {
                source = "balance",
                reason = reason,
                amount = amountInPesewas,
                recipient = recipientCode,
                reference = reference
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.paystack.co/transfer",
                payload);

            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paystack transfer failed: {Error}", content);
                return (false, null, content);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (result.TryGetProperty("data", out var data))
            {
                var transferCode = data.TryGetProperty("transfer_code", out var tc) ? tc.GetString() : null;
                var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;

                _logger.LogInformation(
                    "Paystack transfer initiated: Ref={Reference}, Status={Status}, Code={Code}",
                    reference, status, transferCode);

                return (true, transferCode, status);
            }

            return (false, null, "Invalid Paystack response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Paystack transfer");
            return (false, null, ex.Message);
        }
    }

    public async Task<bool> ExecutePayoutAsync(Guid payoutId, string paymentMethod, Dictionary<string, string> paymentDetails)
    {
        try
        {
            return paymentMethod.ToLower() switch
            {
                "bank_transfer" => await ExecuteBankTransferAsync(payoutId, paymentDetails),
                "momo" => await ExecuteMoMoPayoutAsync(payoutId, paymentDetails),
                "stripe" => await ExecuteStripePayoutAsync(payoutId, paymentDetails),
                _ => throw new ArgumentException($"Unsupported payment method: {paymentMethod}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payout {PayoutId} via {PaymentMethod}", payoutId, paymentMethod);
            return false;
        }
    }

    private async Task<bool> ExecuteBankTransferAsync(Guid payoutId, Dictionary<string, string> paymentDetails)
    {
        if (!paymentDetails.ContainsKey("accountNumber") || 
            !paymentDetails.ContainsKey("bankCode") ||
            !paymentDetails.ContainsKey("amount"))
        {
            _logger.LogError("Missing required bank transfer details for payout {PayoutId}", payoutId);
            return false;
        }

        var accountName = paymentDetails.GetValueOrDefault("accountName", "Owner");
        var ownerEmail = paymentDetails.GetValueOrDefault("email", "owner@example.com");
        var amount = decimal.Parse(paymentDetails["amount"]);
        var reference = $"PAYOUT_{payoutId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Check if recipient code already exists
        string? recipientCode = paymentDetails.GetValueOrDefault("paystackRecipientCode");

        // Create recipient if not exists
        if (string.IsNullOrWhiteSpace(recipientCode))
        {
            _logger.LogInformation("Creating Paystack bank recipient for payout {PayoutId}", payoutId);
            
            recipientCode = await CreatePaystackRecipientAsync(
                ownerEmail,
                paymentDetails["accountNumber"],
                paymentDetails["bankCode"],
                accountName,
                "nuban");

            if (string.IsNullOrWhiteSpace(recipientCode))
            {
                _logger.LogError("Failed to create Paystack recipient for payout {PayoutId}", payoutId);
                return false;
            }
        }

        // Initiate transfer
        var (success, transferCode, status) = await InitiatePaystackTransferAsync(
            recipientCode,
            amount,
            reference,
            paymentDetails.GetValueOrDefault("narration", $"Rental earnings payout - {ownerEmail}"));

        if (success)
        {
            _logger.LogInformation(
                "Bank transfer via Paystack successful for payout {PayoutId}. Code: {Code}, Status: {Status}",
                payoutId, transferCode, status);
            return true;
        }
        else
        {
            _logger.LogError("Bank transfer via Paystack failed for payout {PayoutId}: {Status}", payoutId, status);
            return false;
        }
    }

    private async Task<bool> ExecuteMoMoPayoutAsync(Guid payoutId, Dictionary<string, string> paymentDetails)
    {
        // Paystack Transfer API integration for mobile money
        if (!paymentDetails.ContainsKey("phoneNumber") || 
            !paymentDetails.ContainsKey("amount"))
        {
            _logger.LogError("Missing required MoMo details for payout {PayoutId}", payoutId);
            return false;
        }

        var provider = paymentDetails.GetValueOrDefault("provider", "mtn")?.ToLower();
        var accountName = paymentDetails.GetValueOrDefault("accountName", "Owner");
        var ownerEmail = paymentDetails.GetValueOrDefault("email", "owner@example.com");
        var phoneNumber = paymentDetails["phoneNumber"];
        var amount = decimal.Parse(paymentDetails["amount"]);
        var reference = $"PAYOUT_{payoutId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Map provider to Paystack mobile money bank code
        var bankCode = provider switch
        {
            "mtn" => "MTN",
            "vodafone" => "VOD",
            "airteltigo" => "ATL",
            _ => "MTN"
        };

        // Check if recipient code already exists
        string? recipientCode = paymentDetails.GetValueOrDefault("paystackRecipientCode");

        // Create mobile money recipient if not exists
        if (string.IsNullOrWhiteSpace(recipientCode))
        {
            _logger.LogInformation("Creating Paystack mobile money recipient for payout {PayoutId}, Provider: {Provider}", payoutId, provider);
            
            recipientCode = await CreatePaystackRecipientAsync(
                ownerEmail,
                phoneNumber,
                bankCode,
                accountName,
                "mobile_money");

            if (string.IsNullOrWhiteSpace(recipientCode))
            {
                _logger.LogError("Failed to create Paystack mobile money recipient for payout {PayoutId}", payoutId);
                return false;
            }
        }

        // Initiate transfer
        var (success, transferCode, status) = await InitiatePaystackTransferAsync(
            recipientCode,
            amount,
            reference,
            paymentDetails.GetValueOrDefault("narration", $"Rental earnings payout - {ownerEmail}"));

        if (success)
        {
            _logger.LogInformation(
                "MoMo payout via Paystack successful for payout {PayoutId}. Code: {Code}, Status: {Status}, Provider: {Provider}",
                payoutId, transferCode, status, provider);
            return true;
        }
        else
        {
            _logger.LogError("MoMo payout via Paystack failed for payout {PayoutId}: {Status}", payoutId, status);
            return false;
        }
    }

    private async Task<bool> ExecuteStripePayoutAsync(Guid payoutId, Dictionary<string, string> paymentDetails)
    {
        // Use Stripe Connect for payouts to connected accounts
        
        if (!paymentDetails.ContainsKey("stripeAccountId") || 
            !paymentDetails.ContainsKey("amount"))
        {
            _logger.LogError("Missing required Stripe details for payout {PayoutId}", payoutId);
            return false;
        }

        var secretKey = _configuration["Payment:Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Stripe not configured. Payout {PayoutId} not executed", payoutId);
            return false;
        }

        // Stripe Transfer API
        var url = "https://api.stripe.com/v1/transfers";
        
        var formData = new Dictionary<string, string>
        {
            ["amount"] = ((int)(decimal.Parse(paymentDetails["amount"]) * 100)).ToString(),
            ["currency"] = "ghs",
            ["destination"] = paymentDetails["stripeAccountId"],
            ["transfer_group"] = $"PAYOUT_{payoutId}"
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

        var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(url, content);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Stripe payout executed successfully for payout {PayoutId}", payoutId);
            return true;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Stripe payout failed for payout {PayoutId}: {Error}", payoutId, error);
            return false;
        }
    }
}

public class FakePayoutExecutionService : IPayoutExecutionService
{
    private readonly ILogger<FakePayoutExecutionService> _logger;

    public FakePayoutExecutionService(ILogger<FakePayoutExecutionService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ExecutePayoutAsync(Guid payoutId, string paymentMethod, Dictionary<string, string> paymentDetails)
    {
        _logger.LogInformation(
            "[Payout dev] Executing payout {PayoutId} via {PaymentMethod}: {Details}",
            payoutId,
            paymentMethod,
            string.Join(", ", paymentDetails.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        
        return Task.FromResult(true);
    }
}
