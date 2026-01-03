using System.Text.Json;

namespace GhanaHybridRentalApi.Services;

public class ResendEmailService : IEmailService
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly HttpClient _httpClient;

    public ResendEmailService(IAppConfigService configService, ILogger<ResendEmailService> logger)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = new HttpClient 
        { 
            BaseAddress = new Uri("https://api.resend.com"),
            Timeout = TimeSpan.FromSeconds(30) 
        };
    }

    private async Task SendAsync(string toEmail, string subject, string body)
    {
        var apiKey = await _configService.GetConfigValueAsync("Email:Resend:ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Resend API key not configured");
            throw new InvalidOperationException("Resend API key not configured");
        }

        var from = await _configService.GetConfigValueAsync("Email:Resend:From")
                   ?? "Ryve Rental <no-reply@ryverental.info>";

        var replyTo = await _configService.GetConfigValueAsync("Email:ReplyTo") 
                      ?? "support@ryverental.com";

        var payload = new
        {
            from,
            to = new[] { toEmail },
            subject,
            html = body, // Send body as-is (already HTML from templates)
            reply_to = replyTo
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/emails");
        req.Headers.Add("Authorization", $"Bearer {apiKey}");
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload), 
            System.Text.Encoding.UTF8, 
            "application/json");

        _logger.LogInformation("Sending email via Resend to {Email}", toEmail);

        var resp = await _httpClient.SendAsync(req);
        var respBody = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Resend send failed: {StatusCode} {Response}", (int)resp.StatusCode, respBody);
            throw new InvalidOperationException($"Resend send failed: {(int)resp.StatusCode} {respBody}");
        }

        _logger.LogInformation("Resend email sent successfully to {Email}. Response: {Response}", toEmail, respBody);
    }

    public async Task SendVerificationEmailAsync(string email, string verificationLink)
    {
        var subject = "Verify Your Email - Ryve Rental";
        var body = $@"Hello,

Thank you for registering with Ryve Rental!

Please verify your email address by clicking the link below:
{verificationLink}

This link will expire in 24 hours.

If you didn't create an account, please ignore this email.

Best regards,
Ryve Rental Team";

        await SendAsync(email, subject, body);
    }

    public async Task SendBookingConfirmationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Confirmation - Ryve Rental";
        var body = $@"Hello,

Your booking has been confirmed!

{bookingDetails}

Thank you for choosing Ryve Rental.

Best regards,
Ryve Rental Team";

        await SendAsync(email, subject, body);
    }

    public async Task SendBookingCancellationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Cancellation - Ryve Rental";
        var body = $@"Hello,

Your booking has been cancelled.

{bookingDetails}

If you have any questions, please contact our support team.

Best regards,
Ryve Rental Team";

        await SendAsync(email, subject, body);
    }

    public async Task SendPayoutNotificationAsync(string email, decimal amount)
    {
        var subject = "Payout Notification - Ryve Rental";
        var body = $@"Hello,

A payout of GHS {amount:N2} has been processed to your account.

The funds should arrive within 1-3 business days.

Best regards,
Ryve Rental Team";

        await SendAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password - Ryve Rental";
        var body = $@"Hello,

You requested to reset your password.

Click the link below to reset your password:
{resetLink}

This link will expire in 1 hour.

If you didn't request this, please ignore this email.

Best regards,
Ryve Rental Team";

        await SendAsync(email, subject, body);
    }

    public Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        return SendAsync(email, subject, htmlBody);
    }
}
