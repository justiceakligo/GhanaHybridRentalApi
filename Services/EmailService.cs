using Azure.Communication.Email;

namespace GhanaHybridRentalApi.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string verificationLink);
    Task SendBookingConfirmationAsync(string email, string bookingDetails);
    Task SendBookingCancellationAsync(string email, string bookingDetails);
    Task SendPayoutNotificationAsync(string email, decimal amount);
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendEmailAsync(string email, string subject, string htmlBody);
    Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlBody, List<EmailAttachment> attachments);
}

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string email, string verificationLink)
    {
        _logger.LogInformation("FAKE EMAIL: Verification email to {Email}: {Link}", email, verificationLink);
        return Task.CompletedTask;
    }

    public Task SendBookingConfirmationAsync(string email, string bookingDetails)
    {
        _logger.LogInformation("FAKE EMAIL: Booking confirmation to {Email}: {Details}", email, bookingDetails);
        return Task.CompletedTask;
    }

    public Task SendBookingCancellationAsync(string email, string bookingDetails)
    {
        _logger.LogInformation("FAKE EMAIL: Booking cancellation to {Email}: {Details}", email, bookingDetails);
        return Task.CompletedTask;
    }

    public Task SendPayoutNotificationAsync(string email, decimal amount)
    {
        _logger.LogInformation("FAKE EMAIL: Payout notification to {Email}: {Amount}", email, amount);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        _logger.LogInformation("FAKE EMAIL: Password reset to {Email}: {Link}", email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        _logger.LogInformation("FAKE EMAIL: To {Email}, Subject: {Subject}", email, subject);
        return Task.CompletedTask;
    }
    public Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment> attachments)
    {
        _logger.LogInformation("FAKE EMAIL: To {Email}, Subject: {Subject}, Attachments: {Count}", email, subject, attachments.Count);
        return Task.CompletedTask;
    }}

public class SmtpEmailService : IEmailService
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IAppConfigService configService, ILogger<SmtpEmailService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    private async Task<(string host, int port, string username, string password, bool useSsl, string fromEmail, string fromName)?> GetSmtpSettingsAsync()
    {
        try
        {
            var host = await _configService.GetConfigValueAsync("Email:Smtp:Host");
            var portStr = await _configService.GetConfigValueAsync("Email:Smtp:Port");
            var username = await _configService.GetConfigValueAsync("Email:Smtp:Username");
            var password = await _configService.GetConfigValueAsync("Email:Smtp:Password");
            var useSslStr = await _configService.GetConfigValueAsync("Email:Smtp:UseSsl") ?? "true";
            var fromEmail = await _configService.GetConfigValueAsync("Email:Smtp:FromEmail") ?? username;
            var fromName = await _configService.GetConfigValueAsync("Email:Smtp:FromName") ?? "Ryve Rental";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            if (!int.TryParse(portStr, out int port))
            {
                port = 587; // Default SMTP port
            }

            bool.TryParse(useSslStr, out bool useSsl);

            return (host, port, username, password, useSsl, fromEmail!, fromName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SMTP settings");
            return null;
        }
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var settings = await GetSmtpSettingsAsync();
        
        if (settings == null)
        {
            _logger.LogWarning("SMTP not configured. Cannot send email to {Email}", toEmail);
            return;
        }

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            
            _logger.LogInformation("Attempting to send email to {Email} via {Host}:{Port} (SSL={UseSsl})", 
                toEmail, settings.Value.host, settings.Value.port, settings.Value.useSsl);
            
            await client.ConnectAsync(settings.Value.host, settings.Value.port, settings.Value.useSsl);
            await client.AuthenticateAsync(settings.Value.username, settings.Value.password);

            var message = new MimeKit.MimeMessage();
            
            // Use fromEmail from settings, fallback to username
            var senderAddress = new MimeKit.MailboxAddress(settings.Value.fromName, settings.Value.fromEmail);
            message.From.Add(senderAddress);
            
            // Add Reply-To header for better user experience
            var replyTo = await _configService.GetConfigValueAsync("Email:ReplyTo") ?? "support@ryverental.com";
            message.ReplyTo.Add(MimeKit.MailboxAddress.Parse(replyTo));
            message.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            
            // Ensure proper headers are set
            message.Date = DateTimeOffset.UtcNow;
            message.MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId();
            
            // Add additional headers for better deliverability
            message.Headers.Add("X-Mailer", "Ryve Rental System");
            message.Headers.Add("X-Priority", "3");
            
            // Use HTML format for better deliverability
            var builder = new MimeKit.BodyBuilder();
            
            // If body already contains HTML DOCTYPE, use it as-is
            if (body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || 
                body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                builder.HtmlBody = body;
            }
            else
            {
                // Wrap plain text in HTML
                builder.HtmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    {System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br>")}
</body>
</html>";
            }
            
            message.Body = builder.ToMessageBody();

            _logger.LogInformation("Sending email - From: {From}, To: {To}, Subject: {Subject}, MessageId: {MessageId}, BodyLength: {BodyLength}", 
                settings.Value.username, toEmail, subject, message.MessageId, body.Length);
            
            // Log the full message for debugging
            using (var ms = new System.IO.MemoryStream())
            {
                await message.WriteToAsync(ms);
                var messageText = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                _logger.LogInformation("Full email message (first 500 chars): {MessagePreview}", 
                    messageText.Length > 500 ? messageText.Substring(0, 500) : messageText);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
    public async Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment> attachments)
    {
        _logger.LogWarning("SMTP does not support attachments. Sending email without attachments to {Email}", email);
        await SendEmailAsync(email, subject, htmlMessage);
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
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingConfirmationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Confirmation - Ryve Rental";
        var body = $@"Hello,

Your booking has been confirmed!

{bookingDetails}

We look forward to serving you.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingCancellationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Cancellation - Ryve Rental";
        var body = $@"Hello,

Your booking has been cancelled.

{bookingDetails}

If you have any questions, please contact us.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPayoutNotificationAsync(string email, decimal amount)
    {
        var subject = "Payout Notification - Ryve Rental";
        var body = $@"Hello,

A payout of GHS {amount:N2} has been processed to your account.

Please allow 1-3 business days for the funds to reflect in your account.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password - Ryve Rental";
        var body = $@"Hello,

You requested to reset your password for your Ryve Rental account.

Click the link below to reset your password:
{resetLink}

This link will expire in 1 hour.

If you didn't request this, please ignore this email.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }
}

// Azure Communication Services Email Service
public class AzureEmailService : IEmailService
{
    private readonly IAppConfigService _configService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureEmailService> _logger;

    public AzureEmailService(IConfiguration configuration, IAppConfigService configService, ILogger<AzureEmailService> logger)
    {
        _configuration = configuration;
        _configService = configService;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Read connection details from AppConfigs (DB) first, then fall back to IConfiguration
            var connectionString = await _configService.GetConfigValueAsync("Azure:Communication:ConnectionString") ?? _configuration["Azure:Communication:ConnectionString"];
            var senderAddress = await _configService.GetConfigValueAsync("Azure:Communication:SenderAddress") ?? _configuration["Azure:Communication:SenderAddress"];

            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress))
            {
                throw new InvalidOperationException("Azure Communication Services not configured (missing connection string or sender)");
            }

            _logger.LogInformation("Sending email via Azure Communication Services to {Email} using sender {Sender}", toEmail, senderAddress);

            // If body already contains HTML DOCTYPE, use it as-is; otherwise wrap in HTML
            var htmlBody = body;
            if (!body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) && 
                !body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                htmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    {System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br>")}
</body>
</html>";
            }

            var emailContent = new EmailContent(subject)
            {
                PlainText = body,
                Html = htmlBody
            };

            var client = new EmailClient(connectionString);

            var emailMessage = new EmailMessage(
                senderAddress: senderAddress,
                content: emailContent,
                recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toEmail) }));

            var emailSendOperation = await client.SendAsync(Azure.WaitUntil.Started, emailMessage);
            
            _logger.LogInformation("Azure email sent successfully to {Email}, MessageId: {MessageId}", toEmail, emailSendOperation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Azure Communication Services to {Email}", toEmail);
            throw;
        }
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
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingConfirmationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Confirmation - Ryve Rental";
        var body = $@"Hello,

Your booking has been confirmed!

{bookingDetails}

We look forward to serving you.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendBookingCancellationAsync(string email, string bookingDetails)
    {
        var subject = "Booking Cancellation - Ryve Rental";
        var body = $@"Hello,

Your booking has been cancelled.

{bookingDetails}

If you have any questions, please contact us.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPayoutNotificationAsync(string email, decimal amount)
    {
        var subject = "Payout Notification - Ryve Rental";
        var body = $@"Hello,

A payout of GHS {amount:N2} has been processed to your account.

Please allow 1-3 business days for the funds to reflect in your account.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password - Ryve Rental";
        var body = $@"Hello,

You requested to reset your password for your Ryve Rental account.

Click the link below to reset your password:
{resetLink}

This link will expire in 1 hour.

If you didn't request this, please ignore this email.

Best regards,
The Ryve Rental Team";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment> attachments)
    {
        _logger.LogWarning("Azure Email Service does not support attachments. Sending email without attachments to {Email}", email);
        await SendEmailAsync(email, subject, htmlMessage);
    }
}

public class PostmarkEmailService : IEmailService
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<PostmarkEmailService> _logger;
    private readonly HttpClient _httpClient;

    public PostmarkEmailService(IAppConfigService configService, ILogger<PostmarkEmailService> logger)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    private async Task SendEmailUsingPostmarkAsync(string toEmail, string subject, string body)
    {
        var apiKey = await _configService.GetConfigValueAsync("Email:Postmark:ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Postmark API key not configured");

        var fromAddress = await _configService.GetConfigValueAsync("Email:Smtp:FromEmail") ?? "support@ryverental.com";
        var messageStream = await _configService.GetConfigValueAsync("Email:Postmark:MessageStream");

        // If body already contains HTML, use it as-is; otherwise wrap plain text in HTML
        var htmlBody = body;
        if (!body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) && 
            !body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            htmlBody = $"<!DOCTYPE html><html><body>{System.Net.WebUtility.HtmlEncode(body).Replace("\n","<br>")}</body></html>";
        }

        var payload = new {
            From = fromAddress,
            To = toEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = body
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Postmark-Server-Token", apiKey);
        if (!string.IsNullOrWhiteSpace(messageStream))
        {
            request.Headers.Add("X-PM-Message-Stream", messageStream);
        }

        var resp = await _httpClient.SendAsync(request);
        var respBody = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Postmark send failed to {Email}. Status: {Status}, Response: {Response}", toEmail, resp.StatusCode, respBody);
            throw new InvalidOperationException($"Postmark send failed: {resp.StatusCode} {respBody}");
        }

        _logger.LogInformation("Postmark email sent successfully to {Email}. Response: {Response}", toEmail, respBody);
    }

    public Task SendVerificationEmailAsync(string email, string verificationLink) =>
        SendEmailUsingPostmarkAsync(email, "Verify Your Email - Ryve Rental", $"Hello,\n\nPlease verify your email by clicking: {verificationLink}\n\nThanks");

    public Task SendBookingConfirmationAsync(string email, string bookingDetails) =>
        SendEmailUsingPostmarkAsync(email, "Booking Confirmation - Ryve Rental", bookingDetails);

    public Task SendBookingCancellationAsync(string email, string bookingDetails) =>
        SendEmailUsingPostmarkAsync(email, "Booking Cancellation - Ryve Rental", bookingDetails);

    public Task SendPayoutNotificationAsync(string email, decimal amount) =>
        SendEmailUsingPostmarkAsync(email, "Payout Notification - Ryve Rental", $"A payout of GHS {amount:N2} has been processed to your account.");

    public Task SendPasswordResetEmailAsync(string email, string resetLink) =>
        SendEmailUsingPostmarkAsync(email, "Reset Your Password - Ryve Rental", $"Click to reset your password: {resetLink}");

    public Task SendEmailAsync(string email, string subject, string htmlBody) =>
        SendEmailUsingPostmarkAsync(email, subject, htmlBody);

    public async Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment> attachments)
    {
        _logger.LogWarning("Postmark does not support attachments in this implementation. Sending email without attachments to {Email}", email);
        await SendEmailAsync(email, subject, htmlMessage);
    }
}

// Composite Email Service - tries Postmark first, then Azure, falls back to SMTP
public class CompositeEmailService : IEmailService
{
    private readonly ILogger<CompositeEmailService> _logger;
    private readonly ResendEmailService? _resendService;
    private readonly PostmarkEmailService? _postmarkService;
    private readonly AzureEmailService? _azureService;
    private readonly SmtpEmailService _smtpService;

    public CompositeEmailService(
        IConfiguration configuration,
        IAppConfigService configService,
        ILogger<CompositeEmailService> logger,
        ILogger<ResendEmailService> resendLogger,
        ILogger<PostmarkEmailService> postmarkLogger,
        ILogger<AzureEmailService> azureLogger,
        ILogger<SmtpEmailService> smtpLogger)
    {
        _logger = logger;
        _smtpService = new SmtpEmailService(configService, smtpLogger);

        // Try to initialize Resend service (preferred)
        try
        {
            var useResend = configService.GetConfigValueAsync<bool>("Email:UseResend", defaultValue: true).GetAwaiter().GetResult();
            if (useResend)
            {
                _resendService = new ResendEmailService(configService, resendLogger);
                _logger.LogInformation("Resend service initialized");
            }
            else
            {
                _logger.LogInformation("Resend disabled via configuration");
                _resendService = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Resend not available, will try Postmark/Azure/SMTP");
            _resendService = null;
        }

        // Try to initialize Postmark service (optional)
        try
        {
            var usePostmark = true;
            try { usePostmark = bool.TryParse(configService.GetConfigValueAsync("Email:UsePostmark").GetAwaiter().GetResult(), out var up) && up; } catch { }
            if (usePostmark)
            {
                _postmarkService = new PostmarkEmailService(configService, postmarkLogger);
                _logger.LogInformation("Postmark service initialized");
            }
            else
            {
                _logger.LogInformation("Postmark disabled via configuration");
                _postmarkService = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Postmark not available, will skip to Azure or SMTP");
            _postmarkService = null;
        }

        // Try to initialize Azure service
        try
        {
            _azureService = new AzureEmailService(configuration, configService, azureLogger);
            _logger.LogInformation("Azure Communication Services initialized successfully (using AppConfigService if available)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Communication Services not available, will use SMTP only");
            _azureService = null;
        }
    }

    private async Task SendWithFallbackAsync(Func<IEmailService, Task> sendAction, string email, string purpose)
    {
        // Try Azure first if available
        if (_azureService != null)
        {
            try
            {
                _logger.LogInformation("Attempting to send {Purpose} email to {Email} via Azure", purpose, email);
                await sendAction(_azureService);
                _logger.LogInformation("Successfully sent {Purpose} email to {Email} via Azure", purpose, email);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure email failed for {Email}, falling back to SMTP", email);
            }
        }

        // Fallback to SMTP
        _logger.LogInformation("Sending {Purpose} email to {Email} via SMTP fallback", purpose, email);
        await sendAction(_smtpService);
    }

    private async Task SendWithFallbackOrderAsync(Func<IEmailService, Task> sendAction, string email, string purpose)
    {
        // Try Resend first if available (preferred service)
        if (_resendService != null)
        {
            try
            {
                _logger.LogInformation("Attempting to send {Purpose} email to {Email} via Resend", purpose, email);
                await sendAction(_resendService);
                _logger.LogInformation("Successfully sent {Purpose} email to {Email} via Resend", purpose, email);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Resend email failed for {Email}, falling back", email);
            }
        }

        // Try Postmark second if available
        if (_postmarkService != null)
        {
            try
            {
                _logger.LogInformation("Attempting to send {Purpose} email to {Email} via Postmark", purpose, email);
                await sendAction(_postmarkService);
                _logger.LogInformation("Successfully sent {Purpose} email to {Email} via Postmark", purpose, email);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Postmark email failed for {Email}, falling back", email);
            }
        }

        // Try Azure next if available
        if (_azureService != null)
        {
            try
            {
                _logger.LogInformation("Attempting to send {Purpose} email to {Email} via Azure", purpose, email);
                await sendAction(_azureService);
                _logger.LogInformation("Successfully sent {Purpose} email to {Email} via Azure", purpose, email);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure email failed for {Email}, falling back to SMTP", email);
            }
        }

        // Fallback to SMTP
        _logger.LogInformation("Sending {Purpose} email to {Email} via SMTP fallback", purpose, email);
        await sendAction(_smtpService);
    }

    public Task SendVerificationEmailAsync(string email, string verificationLink) =>
        SendWithFallbackOrderAsync(s => s.SendVerificationEmailAsync(email, verificationLink), email, "verification");

    public Task SendBookingConfirmationAsync(string email, string bookingDetails) =>
        SendWithFallbackOrderAsync(s => s.SendBookingConfirmationAsync(email, bookingDetails), email, "booking confirmation");

    public Task SendBookingCancellationAsync(string email, string bookingDetails) =>
        SendWithFallbackOrderAsync(s => s.SendBookingCancellationAsync(email, bookingDetails), email, "booking cancellation");

    public Task SendPayoutNotificationAsync(string email, decimal amount) =>
        SendWithFallbackOrderAsync(s => s.SendPayoutNotificationAsync(email, amount), email, "payout notification");

    public Task SendPasswordResetEmailAsync(string email, string resetLink) =>
        SendWithFallbackOrderAsync(s => s.SendPasswordResetEmailAsync(email, resetLink), email, "password reset");

    public Task SendEmailAsync(string email, string subject, string htmlBody) =>
        SendWithFallbackOrderAsync(s => s.SendEmailAsync(email, subject, htmlBody), email, "custom email");

    public async Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment> attachments)
    {
        // Try Resend service for attachments if available
        if (_resendService != null)
        {
            try
            {
                await _resendService.SendEmailWithAttachmentsAsync(email, subject, htmlMessage, attachments);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email with attachments via Resend. Falling back to email without attachments.");
            }
        }
        
        // Fallback: send without attachments
        _logger.LogWarning("Sending email without attachments to {Email} (Resend not available or failed)", email);
        await SendEmailAsync(email, subject, htmlMessage);
    }
}
