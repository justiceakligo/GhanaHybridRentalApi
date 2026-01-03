namespace GhanaHybridRentalApi.Services;

public interface IWhatsAppSender
{
    Task SendVerificationCodeAsync(string phone, string message);
    Task SendBookingNotificationAsync(string phone, string message);
}

/// <summary>
/// Simple dev implementation that just logs to console.
/// </summary>
public class FakeWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<FakeWhatsAppSender> _logger;

    public FakeWhatsAppSender(ILogger<FakeWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(string phone, string message)
    {
        _logger.LogInformation("[WhatsApp dev] To {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }

    public Task SendBookingNotificationAsync(string phone, string message)
    {
        _logger.LogInformation("[WhatsApp dev] To {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// WhatsApp Cloud API (Meta Business API) - Direct integration
/// https://developers.facebook.com/docs/whatsapp/cloud-api
/// </summary>
public class WhatsAppCloudApiSender : IWhatsAppSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppCloudApiSender> _logger;
    private readonly HttpClient _httpClient;

    public WhatsAppCloudApiSender(IConfiguration configuration, ILogger<WhatsAppCloudApiSender> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendVerificationCodeAsync(string phone, string message)
    {
        var accessToken = _configuration["WhatsApp:CloudApi:AccessToken"];
        var phoneNumberId = _configuration["WhatsApp:CloudApi:PhoneNumberId"];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId))
        {
            _logger.LogWarning("WhatsApp Cloud API not configured. Message to {Phone}", phone);
            return;
        }

        try
        {
            var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";
            
            var payload = new
            {
                messaging_product = "whatsapp",
                to = NormalizePhoneNumber(phone),
                type = "text",
                text = new { body = message }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message sent successfully to {Phone}", phone);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send WhatsApp: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp to {Phone}", phone);
        }
    }

    public async Task SendBookingNotificationAsync(string phone, string message)
    {
        await SendVerificationCodeAsync(phone, message);
    }

    private string NormalizePhoneNumber(string phone)
    {
        // Remove spaces, dashes, and ensure international format
        var normalized = phone.Replace(" ", "").Replace("-", "").Replace("+", "");
        
        // Add country code if missing (Ghana = 233)
        if (!normalized.StartsWith("233") && normalized.StartsWith("0"))
        {
            normalized = "233" + normalized.Substring(1);
        }
        
        return normalized;
    }
}

