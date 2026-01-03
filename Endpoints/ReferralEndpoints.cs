using System.Security.Claims;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class ReferralEndpoints
{
    public static void MapReferralEndpoints(this IEndpointRouteBuilder app)
    {
        // Public referral tracking
        app.MapPost("/api/v1/referrals/track", TrackReferralAsync);

        // User referrals
        app.MapGet("/api/v1/referrals/my-referrals", GetMyReferralsAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/referrals/apply", ApplyReferralCodeAsync)
            .RequireAuthorization();

        // Admin referral management
        app.MapGet("/api/v1/admin/referrals", GetAllReferralsAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPut("/api/v1/admin/referrals/{referralId:guid}/complete", CompleteReferralAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> TrackReferralAsync(
        [FromBody] CreateReferralRequest request,
        AppDbContext db)
    {
        // Check if referral code exists
        var partner = await db.IntegrationPartners
            .FirstOrDefaultAsync(p => p.ReferralCode == request.ReferralCode && p.Active);

        if (partner is null)
        {
            // Check if it's a user referral code
            var referrerExists = await db.Users.AnyAsync(u => u.Email == request.ReferralCode || u.Phone == request.ReferralCode);
            if (!referrerExists)
                return Results.BadRequest(new { error = "Invalid referral code" });
        }

        var referral = new Referral
        {
            ReferralCode = request.ReferralCode,
            IntegrationPartnerId = partner?.Id,
            Status = "pending"
        };

        db.Referrals.Add(referral);
        await db.SaveChangesAsync();

        return Results.Ok(new ReferralResponse(
            referral.Id,
            referral.ReferralCode,
            referral.ReferrerUserId,
            referral.ReferredUserId,
            referral.Status,
            referral.CreatedAt,
            referral.CompletedAt,
            referral.RewardAmount
        ));
    }

    private static async Task<IResult> GetMyReferralsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? status)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var query = db.Referrals
            .Where(r => r.ReferrerUserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.ToLowerInvariant());

        var referrals = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var totalRewards = referrals
            .Where(r => r.Status == "rewarded")
            .Sum(r => r.RewardAmount ?? 0);

        return Results.Ok(new
        {
            userId,
            myReferralCode = user.Email ?? user.Phone,
            totalReferrals = referrals.Count,
            completedReferrals = referrals.Count(r => r.Status == "completed" || r.Status == "rewarded"),
            totalRewards,
            referrals = referrals.Select(r => new ReferralResponse(
                r.Id,
                r.ReferralCode,
                r.ReferrerUserId,
                r.ReferredUserId,
                r.Status,
                r.CreatedAt,
                r.CompletedAt,
                r.RewardAmount
            ))
        });
    }

    private static async Task<IResult> ApplyReferralCodeAsync(
        ClaimsPrincipal principal,
        [FromBody] CreateReferralRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Check if user already has a referral
        var existingReferral = await db.Referrals
            .AnyAsync(r => r.ReferredUserId == userId);

        if (existingReferral)
            return Results.BadRequest(new { error = "User already has a referral applied" });

        // Find referrer
        var referrer = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.ReferralCode || u.Phone == request.ReferralCode);

        if (referrer is null)
        {
            // Check integration partner
            var partner = await db.IntegrationPartners
                .FirstOrDefaultAsync(p => p.ReferralCode == request.ReferralCode && p.Active);

            if (partner is null)
                return Results.BadRequest(new { error = "Invalid referral code" });

            var partnerReferral = new Referral
            {
                ReferralCode = request.ReferralCode,
                ReferredUserId = userId,
                IntegrationPartnerId = partner.Id,
                Status = "pending"
            };

            db.Referrals.Add(partnerReferral);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Referral code applied successfully", referralId = partnerReferral.Id });
        }

        if (referrer.Id == userId)
            return Results.BadRequest(new { error = "Cannot refer yourself" });

        var referral = new Referral
        {
            ReferralCode = request.ReferralCode,
            ReferrerUserId = referrer.Id,
            ReferredUserId = userId,
            Status = "pending"
        };

        db.Referrals.Add(referral);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Referral code applied successfully", referralId = referral.Id });
    }

    private static async Task<IResult> GetAllReferralsAsync(
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? integrationPartnerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.Referrals
            .Include(r => r.ReferrerUser)
            .Include(r => r.ReferredUser)
            .Include(r => r.IntegrationPartner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.ToLowerInvariant());

        if (integrationPartnerId.HasValue)
            query = query.Where(r => r.IntegrationPartnerId == integrationPartnerId.Value);

        var total = await query.CountAsync();
        var referrals = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = referrals.Select(r => new
            {
                r.Id,
                r.ReferralCode,
                r.ReferrerUserId,
                referrerEmail = r.ReferrerUser?.Email,
                r.ReferredUserId,
                referredEmail = r.ReferredUser?.Email,
                integrationPartner = r.IntegrationPartner?.Name,
                r.Status,
                r.RewardAmount,
                r.RewardCurrency,
                r.CreatedAt,
                r.CompletedAt
            })
        });
    }

    private static async Task<IResult> CompleteReferralAsync(
        Guid referralId,
        [FromBody] CompleteReferralRequest request,
        AppDbContext db)
    {
        var referral = await db.Referrals.FirstOrDefaultAsync(r => r.Id == referralId);
        if (referral is null)
            return Results.NotFound(new { error = "Referral not found" });

        if (referral.Status == "completed" || referral.Status == "rewarded")
            return Results.BadRequest(new { error = "Referral already completed" });

        referral.Status = "completed";
        referral.CompletedAt = DateTime.UtcNow;

        if (request.RewardAmount.HasValue)
        {
            referral.RewardAmount = request.RewardAmount.Value;
            referral.RewardCurrency = request.RewardCurrency ?? "GHS";
            referral.Status = "rewarded";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            referral.Id,
            referral.Status,
            referral.CompletedAt,
            referral.RewardAmount
        });
    }
}

public record CompleteReferralRequest(decimal? RewardAmount, string? RewardCurrency);
