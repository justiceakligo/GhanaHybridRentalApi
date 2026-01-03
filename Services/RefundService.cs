using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

public interface IRefundService
{
    Task<(bool Success, string? TransferCode, string? ErrorMessage)> ProcessDepositRefundAsync(
        Guid bookingId, 
        Guid renterId, 
        decimal amount, 
        string currency,
        string renterPhone,
        string? renterEmail);
}

public class RefundService : IRefundService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefundService> _logger;
    private readonly Data.AppDbContext _db;

    public RefundService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RefundService> logger,
        Data.AppDbContext db)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    public async Task<(bool Success, string? TransferCode, string? ErrorMessage)> ProcessDepositRefundAsync(
        Guid bookingId,
        Guid renterId,
        decimal amount,
        string currency,
        string renterPhone,
        string? renterEmail)
    {
        try
        {
            // Get Paystack secret key from admin global settings
            var secretKey = await GetPaystackSecretKeyAsync();
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("Paystack secret key not configured");
                return (false, null, "Payment gateway not configured");
            }

            // Normalize phone number
            var normalizedPhone = NormalizePhoneNumber(renterPhone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                _logger.LogError("Invalid phone number for refund: {Phone}", renterPhone);
                return (false, null, "Invalid phone number for refund");
            }

            // Get renter email
            var email = renterEmail ?? "customer@ryverental.info";

            _logger.LogInformation(
                "Processing deposit refund for booking {BookingId}, Amount: {Amount} {Currency}, Phone: {Phone}",
                bookingId, amount, currency, normalizedPhone);

            // Step 1: Create Paystack mobile money recipient
            var recipientCode = await CreatePaystackMobileMoneyRecipientAsync(
                email,
                normalizedPhone,
                "MTN", // Default to MTN, can be enhanced later
                "Deposit Refund",
                secretKey);

            if (string.IsNullOrWhiteSpace(recipientCode))
            {
                _logger.LogError("Failed to create Paystack recipient for booking {BookingId}", bookingId);
                return (false, null, "Failed to create payment recipient");
            }

            // Step 2: Initiate Paystack transfer
            var reference = $"REFUND-{bookingId.ToString("N")[..12].ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var (success, transferCode, status) = await InitiatePaystackTransferAsync(
                recipientCode,
                amount,
                reference,
                $"Deposit refund for booking {bookingId}",
                secretKey);

            if (success)
            {
                _logger.LogInformation(
                    "Deposit refund successful for booking {BookingId}. Transfer: {Code}, Status: {Status}",
                    bookingId, transferCode, status);
                return (true, transferCode, null);
            }
            else
            {
                _logger.LogError("Deposit refund failed for booking {BookingId}: {Status}", bookingId, status);
                return (false, null, status ?? "Transfer failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit refund for booking {BookingId}", bookingId);
            return (false, null, $"Error: {ex.Message}");
        }
    }

    private async Task<string?> GetPaystackSecretKeyAsync()
    {
        try
        {
            var setting = await _db.GlobalSettings
                .FirstOrDefaultAsync(s => s.Key == "PaystackSecretKey");
            
            if (setting == null || string.IsNullOrWhiteSpace(setting.ValueJson))
                return null;
            
            // ValueJson might be a simple string value or JSON
            var json = setting.ValueJson.Trim();
            if (json.StartsWith("\"") && json.EndsWith("\""))
            {
                return JsonSerializer.Deserialize<string>(json);
            }
            return json;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> CreatePaystackMobileMoneyRecipientAsync(
        string email,
        string phoneNumber,
        string bankCode,
        string accountName,
        string secretKey)
    {
        try
        {
            var payload = new
            {
                type = "mobile_money",
                name = accountName,
                account_number = phoneNumber,
                bank_code = bankCode,
                currency = "GHS",
                email = email
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.paystack.co/transferrecipient",
                payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create Paystack mobile money recipient: {Error}", error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (result.TryGetProperty("data", out var data) &&
                data.TryGetProperty("recipient_code", out var code))
            {
                _logger.LogInformation("Created Paystack mobile money recipient: {Code}", code.GetString());
                return code.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Paystack mobile money recipient");
            return null;
        }
    }

    private async Task<(bool Success, string? TransferCode, string? Status)> InitiatePaystackTransferAsync(
        string recipientCode,
        decimal amount,
        string reference,
        string reason,
        string secretKey)
    {
        try
        {
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

            return (false, null, "Invalid response from payment gateway");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating Paystack transfer");
            return (false, null, ex.Message);
        }
    }

    private string? NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        // Remove all non-digit characters
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Ghana numbers: 10 digits starting with 0, or 12 digits starting with 233
        if (digits.Length == 10 && digits.StartsWith("0"))
        {
            return "233" + digits[1..]; // Convert 0244... to 233244...
        }
        else if (digits.Length == 12 && digits.StartsWith("233"))
        {
            return digits;
        }
        else if (digits.Length == 9) // Sometimes entered without leading 0
        {
            return "233" + digits;
        }

        return null; // Invalid format
    }
}
