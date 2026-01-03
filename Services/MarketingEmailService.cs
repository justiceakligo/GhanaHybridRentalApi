using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Services;

public interface IMarketingEmailService
{
    // Send to specific user
    Task<bool> SendMarketingEmailAsync(Guid userId, string templateName, Dictionary<string, string>? customPlaceholders = null);
    
    // Send to multiple users (bulk)
    Task<int> SendBulkMarketingEmailAsync(List<Guid> userIds, string templateName, Dictionary<string, string>? customPlaceholders = null);
    
    // Send to all active users
    Task<int> SendToAllActiveUsersAsync(string templateName, Dictionary<string, string>? customPlaceholders = null);
    
    // Send to users by role (owners, renters, drivers)
    Task<int> SendToUsersByRoleAsync(string role, string templateName, Dictionary<string, string>? customPlaceholders = null);
    
    // Anniversary/milestone emails
    Task<int> SendAccountAnniversaryEmailsAsync();
    Task<int> SendLoyaltyRewardEmailsAsync();
    
    // Newsletter
    Task<int> SendNewsletterAsync(string subject, string content);
}

public class MarketingEmailService : IMarketingEmailService
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailService _emailService;
    private readonly ILogger<MarketingEmailService> _logger;
    private readonly AppDbContext _db;

    public MarketingEmailService(
        IEmailTemplateService emailTemplateService,
        IEmailService emailService,
        ILogger<MarketingEmailService> logger,
        AppDbContext db)
    {
        _emailTemplateService = emailTemplateService;
        _emailService = emailService;
        _logger = logger;
        _db = db;
    }

    public async Task<bool> SendMarketingEmailAsync(Guid userId, string templateName, Dictionary<string, string>? customPlaceholders = null)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("Cannot send marketing email to user {UserId}: User not found or no email", userId);
                return false;
            }

            // Build default placeholders
            var placeholders = new Dictionary<string, string>
            {
                { "user_name", $"{user.FirstName} {user.LastName}".Trim() },
                { "customer_name", $"{user.FirstName} {user.LastName}".Trim() },
                { "email", user.Email },
                { "first_name", user.FirstName ?? "" },
                { "last_name", user.LastName ?? "" },
                { "current_date", DateTime.UtcNow.ToString("MMM dd, yyyy") },
                { "current_year", DateTime.UtcNow.Year.ToString() },
                { "support_email", "support@ryverental.com" },
                { "support_phone", "+233 XX XXX XXXX" },
                { "website_url", "https://ryverental.com" },
                { "dashboard_url", "https://ryverental.com/dashboard" }
            };

            // Merge with custom placeholders
            if (customPlaceholders != null)
            {
                foreach (var kvp in customPlaceholders)
                {
                    placeholders[kvp.Key] = kvp.Value;
                }
            }

            // Render template
            var htmlMessage = await _emailTemplateService.RenderTemplateAsync(templateName, placeholders);
            var subject = await _emailTemplateService.RenderSubjectAsync(templateName, placeholders);

            // Send email
            await _emailService.SendEmailAsync(user.Email, subject, htmlMessage);

            _logger.LogInformation("Sent marketing email '{Template}' to user {UserId}", templateName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send marketing email '{Template}' to user {UserId}", templateName, userId);
            return false;
        }
    }

    public async Task<int> SendBulkMarketingEmailAsync(List<Guid> userIds, string templateName, Dictionary<string, string>? customPlaceholders = null)
    {
        var sentCount = 0;

        foreach (var userId in userIds)
        {
            var success = await SendMarketingEmailAsync(userId, templateName, customPlaceholders);
            if (success)
            {
                sentCount++;
            }

            // Small delay to avoid overwhelming email service
            await Task.Delay(100);
        }

        _logger.LogInformation("Sent marketing email '{Template}' to {SentCount}/{TotalCount} users", 
            templateName, sentCount, userIds.Count);

        return sentCount;
    }

    public async Task<int> SendToAllActiveUsersAsync(string templateName, Dictionary<string, string>? customPlaceholders = null)
    {
        var activeUsers = await _db.Users
            .Where(u => !string.IsNullOrEmpty(u.Email))
            .Select(u => u.Id)
            .ToListAsync();

        return await SendBulkMarketingEmailAsync(activeUsers, templateName, customPlaceholders);
    }

    public async Task<int> SendToUsersByRoleAsync(string role, string templateName, Dictionary<string, string>? customPlaceholders = null)
    {
        var userIds = new List<Guid>();

        switch (role.ToLower())
        {
            case "owner":
            case "owners":
                // Users who have vehicles
                userIds = await _db.Vehicles
                    .Where(v => v.OwnerId != Guid.Empty)
                    .Select(v => v.OwnerId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "renter":
            case "renters":
            case "customer":
            case "customers":
                // Users who have made bookings
                userIds = await _db.Bookings
                    .Where(b => b.RenterId != Guid.Empty)
                    .Select(b => b.RenterId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "driver":
            case "drivers":
                // Users with driver profile
                userIds = await _db.DriverProfiles
                    .Select(dp => dp.UserId)
                    .Distinct()
                    .ToListAsync();
                break;

            default:
                _logger.LogWarning("Unknown role '{Role}' for marketing email", role);
                return 0;
        }

        return await SendBulkMarketingEmailAsync(userIds, templateName, customPlaceholders);
    }

    public async Task<int> SendAccountAnniversaryEmailsAsync()
    {
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var startOfToday = DateTime.UtcNow.Date;
        var endOfToday = startOfToday.AddDays(1);

        // Find users whose accounts are exactly 1 year old today (within the day)
        var anniversaryUsers = await _db.Users
            .Where(u => !string.IsNullOrEmpty(u.Email) &&
                       u.CreatedAt >= oneYearAgo &&
                       u.CreatedAt < oneYearAgo.AddDays(1))
            .ToListAsync();

        var sentCount = 0;

        foreach (var user in anniversaryUsers)
        {
            var yearsActive = (DateTime.UtcNow - user.CreatedAt).Days / 365;
            var placeholders = new Dictionary<string, string>
            {
                { "years_active", yearsActive.ToString() },
                { "member_since", user.CreatedAt.ToString("MMM dd, yyyy") },
                { "anniversary_date", DateTime.UtcNow.ToString("MMM dd, yyyy") }
            };

            var success = await SendMarketingEmailAsync(user.Id, "loyalty_reward", placeholders);
            if (success)
            {
                sentCount++;
            }

            await Task.Delay(100);
        }

        _logger.LogInformation("Sent {Count} account anniversary emails", sentCount);
        return sentCount;
    }

    public async Task<int> SendLoyaltyRewardEmailsAsync()
    {
        // Find users with 5+ completed bookings
        var loyalCustomers = await _db.Bookings
            .Where(b => b.Status == "completed" && b.RenterId != Guid.Empty)
            .GroupBy(b => b.RenterId)
            .Where(g => g.Count() >= 5)
            .Select(g => new { UserId = g.Key, BookingCount = g.Count() })
            .ToListAsync();

        var sentCount = 0;

        foreach (var customer in loyalCustomers)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "total_bookings", customer.BookingCount.ToString() },
                { "reward_type", "10% Discount" },
                { "promo_code", $"LOYAL{customer.UserId.ToString().Substring(0, 6).ToUpper()}" },
                { "valid_until", DateTime.UtcNow.AddMonths(3).ToString("MMM dd, yyyy") }
            };

            var success = await SendMarketingEmailAsync(customer.UserId, "loyalty_reward", placeholders);
            if (success)
            {
                sentCount++;
            }

            await Task.Delay(100);
        }

        _logger.LogInformation("Sent {Count} loyalty reward emails", sentCount);
        return sentCount;
    }

    public async Task<int> SendNewsletterAsync(string subject, string content)
    {
        var activeUsers = await _db.Users
            .Where(u => !string.IsNullOrEmpty(u.Email))
            .ToListAsync();

        var sentCount = 0;

        foreach (var user in activeUsers)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "newsletter_title", subject },
                { "newsletter_content", content },
                { "newsletter_date", DateTime.UtcNow.ToString("MMMM yyyy") }
            };

            var success = await SendMarketingEmailAsync(user.Id, "newsletter", placeholders);
            if (success)
            {
                sentCount++;
            }

            await Task.Delay(100);
        }

        _logger.LogInformation("Sent newsletter '{Subject}' to {Count} users", subject, sentCount);
        return sentCount;
    }
}
