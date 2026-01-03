using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GhanaHybridRentalApi.Endpoints;

public static class MarketingEmailEndpoints
{
    public static void MapMarketingEmailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/marketing")
            .WithTags("Marketing Emails")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,SuperAdmin" });

        // Send to specific user
        group.MapPost("/send-to-user", async (
            Guid userId,
            string templateName,
            [FromBody] Dictionary<string, string>? customPlaceholders,
            IMarketingEmailService marketingService) =>
        {
            var success = await marketingService.SendMarketingEmailAsync(userId, templateName, customPlaceholders);
            return success 
                ? Results.Ok(new { message = "Marketing email sent successfully", userId, templateName })
                : Results.BadRequest(new { error = "Failed to send marketing email" });
        })
        .WithName("SendMarketingEmailToUser")
        .WithSummary("Send marketing email to a specific user");

        // Send to multiple users (bulk)
        group.MapPost("/send-bulk", async ([FromBody] GhanaHybridRentalApi.Dtos.SendBulkMarketingRequest req, IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendBulkMarketingEmailAsync(req.userIds, req.templateName, req.customPlaceholders);
            return Results.Ok(new 
            { 
                message = $"Sent to {sentCount}/{req.userIds.Count} users",
                templateName = req.templateName,
                totalUsers = req.userIds.Count,
                sentCount
            });
        })
        .WithName("SendBulkMarketingEmail")
        .WithSummary("Send marketing email to multiple users");

        // Send to all active users
        group.MapPost("/send-to-all", async (
            string templateName,
            [FromBody] Dictionary<string, string>? customPlaceholders,
            IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendToAllActiveUsersAsync(templateName, customPlaceholders);
            return Results.Ok(new 
            { 
                message = $"Campaign sent to {sentCount} users",
                templateName,
                sentCount
            });
        })
        .WithName("SendMarketingEmailToAll")
        .WithSummary("Send marketing email to all active users");

        // Send to users by role
        group.MapPost("/send-to-role", async (
            string role,
            string templateName,
            [FromBody] Dictionary<string, string>? customPlaceholders,
            IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendToUsersByRoleAsync(role, templateName, customPlaceholders);
            return Results.Ok(new 
            { 
                message = $"Campaign sent to {sentCount} {role}s",
                role,
                templateName,
                sentCount
            });
        })
        .WithName("SendMarketingEmailToRole")
        .WithSummary("Send marketing email to users by role (owners/renters/drivers)");

        // Send account anniversary emails
        group.MapPost("/send-anniversary-emails", async (
            IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendAccountAnniversaryEmailsAsync();
            return Results.Ok(new 
            { 
                message = $"Sent {sentCount} anniversary emails",
                sentCount
            });
        })
        .WithName("SendAccountAnniversaryEmails")
        .WithSummary("Send anniversary emails to users celebrating account milestones");

        // Send loyalty reward emails
        group.MapPost("/send-loyalty-rewards", async (
            IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendLoyaltyRewardEmailsAsync();
            return Results.Ok(new 
            { 
                message = $"Sent {sentCount} loyalty reward emails",
                sentCount
            });
        })
        .WithName("SendLoyaltyRewardEmails")
        .WithSummary("Send loyalty reward emails to frequent customers (5+ bookings)");

        // Send newsletter
        group.MapPost("/send-newsletter", async (
            string subject,
            string content,
            IMarketingEmailService marketingService) =>
        {
            var sentCount = await marketingService.SendNewsletterAsync(subject, content);
            return Results.Ok(new 
            { 
                message = $"Newsletter sent to {sentCount} users",
                subject,
                sentCount
            });
        })
        .WithName("SendNewsletter")
        .WithSummary("Send newsletter to all active users");

        // Campaign examples (pre-configured templates)
        group.MapPost("/campaigns/promotional-offer", async (
            string offerTitle,
            string discountPercentage,
            string promoCode,
            string validUntil,
            string? targetRole,
            IMarketingEmailService marketingService) =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "offer_title", offerTitle },
                { "discount_percentage", discountPercentage },
                { "promo_code", promoCode },
                { "valid_until", validUntil }
            };

            var sentCount = string.IsNullOrEmpty(targetRole)
                ? await marketingService.SendToAllActiveUsersAsync("promotional_offer", placeholders)
                : await marketingService.SendToUsersByRoleAsync(targetRole, "promotional_offer", placeholders);

            return Results.Ok(new 
            { 
                message = $"Promotional offer sent to {sentCount} users",
                sentCount,
                offerTitle,
                promoCode
            });
        })
        .WithName("SendPromotionalOffer")
        .WithSummary("Send promotional offer campaign");

        group.MapPost("/campaigns/seasonal", async (
            string campaignTitle,
            string seasonalMessage,
            string specialOffer,
            string? targetRole,
            IMarketingEmailService marketingService) =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "campaign_title", campaignTitle },
                { "seasonal_message", seasonalMessage },
                { "special_offer", specialOffer },
                { "campaign_period", DateTime.UtcNow.ToString("MMMM yyyy") }
            };

            var sentCount = string.IsNullOrEmpty(targetRole)
                ? await marketingService.SendToAllActiveUsersAsync("seasonal_campaign", placeholders)
                : await marketingService.SendToUsersByRoleAsync(targetRole, "seasonal_campaign", placeholders);

            return Results.Ok(new 
            { 
                message = $"Seasonal campaign sent to {sentCount} users",
                sentCount,
                campaignTitle
            });
        })
        .WithName("SendSeasonalCampaign")
        .WithSummary("Send seasonal campaign (Christmas, New Year, etc.)");

        group.MapPost("/campaigns/referral", async (
            string referralBonus,
            string referredBonus,
            IMarketingEmailService marketingService) =>
        {
            var placeholders = new Dictionary<string, string>
            {
                { "referral_bonus", referralBonus },
                { "referred_bonus", referredBonus },
                { "referral_link", "https://ryverental.com/referral" }
            };

            var sentCount = await marketingService.SendToAllActiveUsersAsync("referral_bonus", placeholders);

            return Results.Ok(new 
            { 
                message = $"Referral campaign sent to {sentCount} users",
                sentCount
            });
        })
        .WithName("SendReferralCampaign")
        .WithSummary("Send referral bonus campaign");
    }
}
