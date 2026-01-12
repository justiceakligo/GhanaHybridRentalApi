using System.Security.Cryptography;
using System.Text;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class IntegrationPartnerEndpoints
{
    public static void MapIntegrationPartnerEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin endpoints
        app.MapGet("/api/v1/admin/integration-partners", GetPartnersAsync)
            .RequireAuthorization("AdminOnly");

        app.MapGet("/api/v1/admin/integration-partners/{partnerId:guid}", GetPartnerAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/integration-partners", CreatePartnerAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPut("/api/v1/admin/integration-partners/{partnerId:guid}", UpdatePartnerAsync)
            .RequireAuthorization("AdminOnly");

        app.MapDelete("/api/v1/admin/integration-partners/{partnerId:guid}", DeletePartnerAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/integration-partners/{partnerId:guid}/regenerate-key", RegenerateApiKeyAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/integration-partners/{partnerId:guid}/set-expiry", SetApiKeyExpiryAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/integration-partners/{partnerId:guid}/renew-key", RenewApiKeyAsync)
            .RequireAuthorization("AdminOnly");

        // Integration Partner application approval/rejection (separate from regular Partners)
        app.MapPost("/api/v1/admin/integration-partner-applications/{partnerId:guid}/approve", ApproveIntegrationPartnerApplicationAsync)
            .RequireAuthorization("AdminOnly");
        
        app.MapPost("/api/v1/admin/integration-partner-applications/{partnerId:guid}/reject", RejectIntegrationPartnerApplicationAsync)
            .RequireAuthorization("AdminOnly");

        // Alias for frontend: get integration partner applications (inactive = pending)
        // Note: This is different from regular Partner applications
        app.MapGet("/api/v1/admin/partner-applications", GetPartnerApplicationsAsync)
            .RequireAuthorization("AdminOnly");

        // Public endpoint for partner application submission
        app.MapPost("/api/v1/partner-applications", SubmitPartnerApplicationAsync)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPartnersAsync(
        AppDbContext db,
        [FromQuery] string? type,
        [FromQuery] bool? active)
    {
        var query = db.IntegrationPartners.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(p => p.Type == type.ToLowerInvariant());

        if (active.HasValue)
            query = query.Where(p => p.Active == active.Value);

        var partners = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Results.Ok(partners.Select(p => new IntegrationPartnerResponse(
            p.Id,
            p.Name,
            p.Type,
            p.ApiKey,
            p.ReferralCode,
            p.WebhookUrl,
            p.Active,
            p.CreatedAt,
            p.LastUsedAt,
            p.ApiKeyExpiresAt,
            p.ContactPerson,
            p.Email,
            p.Phone,
            p.Website,
            p.ApplicationReference,
            p.CommissionPercent,
            p.SettlementTermDays
        )));
    }

    private static async Task<IResult> GetPartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        return Results.Ok(new IntegrationPartnerResponse(
            partner.Id,
            partner.Name,
            partner.Type,
            partner.ApiKey,
            partner.ReferralCode,
            partner.WebhookUrl,
            partner.Active,
            partner.CreatedAt,
            partner.LastUsedAt,
            partner.ApiKeyExpiresAt,
            partner.ContactPerson,
            partner.Email,
            partner.Phone,
            partner.Website,
            partner.ApplicationReference,
            partner.CommissionPercent,
            partner.SettlementTermDays
        ));
    }

    private static async Task<IResult> CreatePartnerAsync(
        [FromBody] CreateIntegrationPartnerRequest request,
        AppDbContext db)
    {
        var validTypes = new[] { "hotel", "travel_agency", "ota", "custom" };
        if (!validTypes.Contains(request.Type.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid partner type" });

        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var existingCode = await db.IntegrationPartners
                .AnyAsync(p => p.ReferralCode == request.ReferralCode);
            if (existingCode)
                return Results.BadRequest(new { error = "Referral code already exists" });
        }

        var apiKey = GenerateApiKey();

        var partner = new IntegrationPartner
        {
            Name = request.Name,
            Type = request.Type.ToLowerInvariant(),
            ApiKey = apiKey,
            ReferralCode = request.ReferralCode,
            WebhookUrl = request.WebhookUrl,
            Active = true
        };

        db.IntegrationPartners.Add(partner);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/admin/integration-partners/{partner.Id}",
            new IntegrationPartnerResponse(
                partner.Id,
                partner.Name,
                partner.Type,
                partner.ApiKey,
                partner.ReferralCode,
                partner.WebhookUrl,
                partner.Active,
                partner.CreatedAt,
                partner.LastUsedAt,
                partner.ApiKeyExpiresAt,
                partner.ContactPerson,
                partner.Email,
                partner.Phone,
                partner.Website,
                partner.ApplicationReference,
                partner.CommissionPercent,
                partner.SettlementTermDays
            ));
    }

    private static async Task<IResult> GetPartnerApplicationsAsync(
        AppDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.IntegrationPartners.Where(p => !p.Active).AsQueryable();

        var total = await query.CountAsync();
        var partners = await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = partners });
    }

    private static async Task<IResult> UpdatePartnerAsync(
        Guid partnerId,
        [FromBody] UpdateIntegrationPartnerRequest request,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        if (!string.IsNullOrWhiteSpace(request.Name))
            partner.Name = request.Name;

        if (request.WebhookUrl is not null)
            partner.WebhookUrl = request.WebhookUrl;

        if (request.Active.HasValue)
            partner.Active = request.Active.Value;

        await db.SaveChangesAsync();

        return Results.Ok(new IntegrationPartnerResponse(
            partner.Id,
            partner.Name,
            partner.Type,
            partner.ApiKey,
            partner.ReferralCode,
            partner.WebhookUrl,
            partner.Active,
            partner.CreatedAt,
            partner.LastUsedAt,
            partner.ApiKeyExpiresAt,
            partner.ContactPerson,
            partner.Email,
            partner.Phone,
            partner.Website,
            partner.ApplicationReference,
            partner.CommissionPercent,
            partner.SettlementTermDays
        ));
    }

    private static async Task<IResult> DeletePartnerAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        db.IntegrationPartners.Remove(partner);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> RegenerateApiKeyAsync(
        Guid partnerId,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        partner.ApiKey = GenerateApiKey();
        await db.SaveChangesAsync();

        return Results.Ok(new { partner.Id, partner.ApiKey });
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"ghr_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private static string GenerateApplicationReference()
    {
        var year = DateTime.UtcNow.Year;
        var random = RandomNumberGenerator.GetInt32(100000, 999999);
        return $"PA-{year}-{random:D6}";
    }

    // Public endpoint for partner application submission
    private static async Task<IResult> SubmitPartnerApplicationAsync(
        [FromBody] PartnerApplicationRequest request,
        AppDbContext db,
        INotificationService notificationService)
    {
        // Validate business type
        var validTypes = new[] { "hotel", "travel_agency", "ota", "tour_operator", "car_rental", "custom" };
        if (!validTypes.Contains(request.BusinessType.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid business type" });

        // Validate email format
        if (!IsValidEmail(request.Email))
            return Results.BadRequest(new { error = "Invalid email address" });

        // Check for duplicate email
        var existingEmail = await db.IntegrationPartners
            .AnyAsync(p => p.Email!.ToLower() == request.Email.ToLower());
        if (existingEmail)
            return Results.BadRequest(new { error = "An application with this email already exists" });

        // Generate unique application reference
        var applicationReference = GenerateApplicationReference();

        var partner = new IntegrationPartner
        {
            Name = request.BusinessName,
            Type = request.BusinessType.ToLowerInvariant(),
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Website = request.Website,
            RegistrationNumber = request.RegistrationNumber,
            Description = request.Description,
            WebhookUrl = request.WebhookUrl,
            ApplicationReference = applicationReference,
            Active = false, // Pending approval
            ApiKey = string.Empty, // Will be generated on approval
            CommissionPercent = 15m, // Default
            SettlementTermDays = 30 // Default
        };

        db.IntegrationPartners.Add(partner);
        await db.SaveChangesAsync();

        // Send application received email (non-blocking)
        try
        {
            await notificationService.SendIntegrationPartnerApplicationReceivedAsync(partner);
        }
        catch (Exception ex)
        {
            // Log and continue - email failures should not block application submission
            // NotificationService already logs failures; keep this as extra safety
        }

        return Results.Created($"/api/v1/partner-applications/{partner.Id}",
            new PartnerApplicationResponse(
                partner.Id,
                applicationReference,
                request.BusinessName,
                "pending",
                partner.CreatedAt,
                $"Your application has been submitted successfully. Reference: {applicationReference}. We'll review and contact you within 2-3 business days."
            ));
    }

    // Admin endpoint to set API key expiry
    private static async Task<IResult> SetApiKeyExpiryAsync(
        Guid partnerId,
        [FromBody] SetApiKeyExpiryRequest request,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        partner.ApiKeyExpiresAt = request.ExpiresAt;
        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            partnerId = partner.Id, 
            apiKeyExpiresAt = partner.ApiKeyExpiresAt,
            message = partner.ApiKeyExpiresAt == null 
                ? "API key expiry removed (no expiry)" 
                : $"API key will expire on {partner.ApiKeyExpiresAt:yyyy-MM-dd HH:mm:ss} UTC"
        });
    }

    // Admin endpoint to renew API key with optional new expiry
    private static async Task<IResult> RenewApiKeyAsync(
        Guid partnerId,
        [FromBody] RenewApiKeyRequest request,
        AppDbContext db)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner not found" });

        // Generate new API key
        partner.ApiKey = GenerateApiKey();

        // Set new expiry if provided
        if (request.ExpiryDays.HasValue)
            partner.ApiKeyExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays.Value);
        else
            partner.ApiKeyExpiresAt = null; // No expiry

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            partner.Id, 
            partner.ApiKey, 
            apiKeyExpiresAt = partner.ApiKeyExpiresAt,
            message = partner.ApiKeyExpiresAt == null
                ? "API key renewed successfully (no expiry)"
                : $"API key renewed successfully. Expires on {partner.ApiKeyExpiresAt:yyyy-MM-dd HH:mm:ss} UTC"
        });
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Approve IntegrationPartner application (different from regular Partner approval)
    private static async Task<IResult> ApproveIntegrationPartnerApplicationAsync(
        Guid partnerId,
        AppDbContext db,
        INotificationService notificationService,
        [FromQuery] int? expiryDays)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner application not found" });

        if (partner.Active)
            return Results.BadRequest(new { error = "Integration partner application already approved" });

        // Generate API key if not already present
        if (string.IsNullOrEmpty(partner.ApiKey))
        {
            partner.ApiKey = GenerateApiKey();
        }

        // Set API key expiry if provided
        if (expiryDays.HasValue && expiryDays.Value > 0)
            partner.ApiKeyExpiresAt = DateTime.UtcNow.AddDays(expiryDays.Value);

        partner.Active = true;
        await db.SaveChangesAsync();

        // Send approval email (non-blocking)
        try
        {
            await notificationService.SendIntegrationPartnerApplicationApprovedAsync(partner, partner.ApiKey);
        }
        catch (Exception ex)
        {
            // Log is handled by NotificationService, continue
        }

        return Results.Ok(new 
        { 
            success = true, 
            message = $"Integration partner '{partner.Name}' approved and activated", 
            apiKey = partner.ApiKey,
            apiKeyExpiresAt = partner.ApiKeyExpiresAt,
            partnerId = partner.Id,
            applicationReference = partner.ApplicationReference
        });
    }

    // Reject IntegrationPartner application (different from regular Partner rejection)
    private static async Task<IResult> RejectIntegrationPartnerApplicationAsync(
        Guid partnerId,
        [FromBody] RejectIntegrationPartnerRequest? request,
        AppDbContext db,
        INotificationService notificationService)
    {
        var partner = await db.IntegrationPartners.FirstOrDefaultAsync(p => p.Id == partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Integration partner application not found" });

        // Store rejection reason in AdminNotes
        if (!string.IsNullOrWhiteSpace(request?.Reason))
        {
            var rejectionNote = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] REJECTED: {request.Reason}";
            partner.AdminNotes = string.IsNullOrWhiteSpace(partner.AdminNotes) 
                ? rejectionNote 
                : $"{partner.AdminNotes}\n{rejectionNote}";
        }

        // Send rejection email before deletion (non-blocking)
        try
        {
            await notificationService.SendIntegrationPartnerApplicationRejectedAsync(partner.Email ?? string.Empty, partner.ApplicationReference ?? string.Empty, request?.Reason ?? string.Empty);
        }
        catch
        {
            // Logging handled by NotificationService
        }

        // Delete the application (rejected applications not kept in production)
        db.IntegrationPartners.Remove(partner);
        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            success = true, 
            message = $"Integration partner application '{partner.Name}' rejected and removed",
            applicationReference = partner.ApplicationReference
        });
    }
}

// DTO for rejecting integration partner applications
public record RejectIntegrationPartnerRequest(string? Reason);
