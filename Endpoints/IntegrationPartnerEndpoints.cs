using System.Security.Cryptography;
using System.Text;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class IntegrationPartnerEndpoints
{
    public static void MapIntegrationPartnerEndpoints(this IEndpointRouteBuilder app)
    {
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

        // Alias for frontend: get partner applications (inactive = pending)
        app.MapGet("/api/v1/admin/partner-applications", GetPartnerApplicationsAsync)
            .RequireAuthorization("AdminOnly");
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
            p.LastUsedAt
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
            partner.LastUsedAt
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
                partner.LastUsedAt
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
            partner.LastUsedAt
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
}
