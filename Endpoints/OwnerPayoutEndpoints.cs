using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GhanaHybridRentalApi.Endpoints;

public static class OwnerPayoutEndpoints
{
    public static void MapOwnerPayoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/owner/payouts").RequireAuthorization();

        // Owner endpoints
        group.MapGet("/settings", GetPayoutSettingsAsync)
            .RequireAuthorization(policy => policy.RequireRole("owner"));

        group.MapPut("/settings", UpdatePayoutSettingsAsync)
            .RequireAuthorization(policy => policy.RequireRole("owner"));

        group.MapPost("/instant-withdrawal", RequestInstantWithdrawalAsync)
            .RequireAuthorization(policy => policy.RequireRole("owner"));

        group.MapGet("/instant-withdrawals", GetInstantWithdrawalsAsync)
            .RequireAuthorization(policy => policy.RequireRole("owner"));

        // Admin endpoints
        var adminGroup = app.MapGroup("/api/v1/admin/payouts").RequireAuthorization();

        adminGroup.MapGet("/scheduled/due", GetScheduledPayoutsDueAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        adminGroup.MapPost("/scheduled/process", ProcessScheduledPayoutsAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        adminGroup.MapGet("/instant-withdrawals/pending", GetPendingInstantWithdrawalsAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));
    }

    private static async Task<IResult> GetPayoutSettingsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var ownerProfile = await db.OwnerProfiles.FirstOrDefaultAsync(o => o.UserId == userId);

        if (ownerProfile == null)
            return Results.NotFound(new { error = "Owner profile not found" });

        var feePercentage = await configService.GetConfigValueAsync<decimal>("Payout:InstantWithdrawalFeePercentage", 3.0m);

        var response = new PayoutSettingsResponse(
            ownerProfile.PayoutFrequency,
            ownerProfile.MinimumPayoutAmount,
            ownerProfile.InstantWithdrawalEnabled,
            feePercentage
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdatePayoutSettingsAsync(
        ClaimsPrincipal principal,
        [FromBody] UpdatePayoutSettingsRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var ownerProfile = await db.OwnerProfiles.FirstOrDefaultAsync(o => o.UserId == userId);

        if (ownerProfile == null)
            return Results.NotFound(new { error = "Owner profile not found" });

        // Validate frequency
        var validFrequencies = new[] { "daily", "weekly", "biweekly", "monthly" };
        if (!validFrequencies.Contains(request.PayoutFrequency.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid payout frequency. Must be: daily, weekly, biweekly, or monthly" });

        ownerProfile.PayoutFrequency = request.PayoutFrequency.ToLowerInvariant();
        ownerProfile.MinimumPayoutAmount = request.MinimumPayoutAmount;
        ownerProfile.InstantWithdrawalEnabled = request.InstantWithdrawalEnabled;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Payout settings updated successfully" });
    }

    private static async Task<IResult> RequestInstantWithdrawalAsync(
        ClaimsPrincipal principal,
        [FromBody] InstantWithdrawalRequest request,
        AppDbContext db,
        IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var ownerProfile = await db.OwnerProfiles.FirstOrDefaultAsync(o => o.UserId == userId);

        if (ownerProfile == null)
            return Results.NotFound(new { error = "Owner profile not found" });

        if (!ownerProfile.InstantWithdrawalEnabled)
            return Results.BadRequest(new { error = "Instant withdrawal is disabled for your account. Please contact support or update your payout settings." });

        if (ownerProfile.PayoutVerificationStatus != "verified")
            return Results.BadRequest(new { error = "Your payout details must be verified before you can withdraw funds" });

        // Calculate available balance
        var completedBookings = await db.Bookings
            .Where(b => b.OwnerId == userId && b.Status == "completed")
            .ToListAsync();

        // Owner receives: rental + driver fees - platform commission
        var totalEarnings = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

        var payouts = await db.Payouts.Where(p => p.OwnerId == userId).ToListAsync();
        var totalPaidOut = payouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
        var pendingPayout = payouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);

        var instantWithdrawals = await db.InstantWithdrawals.Where(w => w.OwnerId == userId).ToListAsync();
        var completedWithdrawals = instantWithdrawals.Where(w => w.Status == "completed").Sum(w => w.Amount);
        var pendingWithdrawals = instantWithdrawals.Where(w => w.Status == "pending" || w.Status == "processing").Sum(w => w.Amount);

        var available = totalEarnings - totalPaidOut - pendingPayout - completedWithdrawals - pendingWithdrawals;

        if (request.Amount > available)
            return Results.BadRequest(new { error = $"Insufficient balance. Available: {available:F2}" });

        // Get withdrawal fee percentage
        var feePercentage = await configService.GetConfigValueAsync<decimal>("Payout:InstantWithdrawalFeePercentage", 3.0m);
        var feeAmount = request.Amount * (feePercentage / 100);
        var netAmount = request.Amount - feeAmount;

        var withdrawal = new InstantWithdrawal
        {
            OwnerId = userId,
            Amount = request.Amount,
            FeeAmount = feeAmount,
            FeePercentage = feePercentage,
            NetAmount = netAmount,
            Currency = "GHS",
            Status = "pending",
            Method = ownerProfile.PayoutPreference,
            PayoutDetailsJson = ownerProfile.PayoutDetailsJson,
            Reference = $"IW-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId.ToString()[..8]}"
        };

        db.InstantWithdrawals.Add(withdrawal);
        await db.SaveChangesAsync();

        // TODO: Integrate with Paystack Transfer API to process instant withdrawal
        // For now, mark as processing and return success

        var response = new InstantWithdrawalResponse(
            withdrawal.Id,
            withdrawal.Amount,
            withdrawal.FeeAmount,
            withdrawal.FeePercentage,
            withdrawal.NetAmount,
            withdrawal.Currency,
            withdrawal.Status,
            withdrawal.Method,
            withdrawal.CreatedAt,
            withdrawal.CompletedAt,
            withdrawal.ErrorMessage
        );

        return Results.Ok(new 
        { 
            message = "Instant withdrawal request submitted successfully. Processing...",
            withdrawal = response
        });
    }

    private static async Task<IResult> GetInstantWithdrawalsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var withdrawals = await db.InstantWithdrawals
            .Where(w => w.OwnerId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var response = withdrawals.Select(w => new InstantWithdrawalResponse(
            w.Id,
            w.Amount,
            w.FeeAmount,
            w.FeePercentage,
            w.NetAmount,
            w.Currency,
            w.Status,
            w.Method,
            w.CreatedAt,
            w.CompletedAt,
            w.ErrorMessage
        )).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetScheduledPayoutsDueAsync(
        AppDbContext db,
        [FromQuery] DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;

        // Get all owners with their profiles
        var owners = await db.Users
            .Include(u => u.OwnerProfile)
            .Where(u => u.Role == "owner" && u.OwnerProfile != null && u.OwnerProfile.PayoutVerificationStatus == "verified")
            .ToListAsync();

        var scheduledPayouts = new List<ScheduledPayoutResponse>();

        foreach (var owner in owners)
        {
            var profile = owner.OwnerProfile!;

            // Get last payout date
            var lastPayout = await db.Payouts
                .Where(p => p.OwnerId == owner.Id && p.Status == "completed")
                .OrderByDescending(p => p.CompletedAt)
                .FirstOrDefaultAsync();

            var lastPayoutDate = lastPayout?.CompletedAt ?? owner.CreatedAt;

            // Calculate next payout date based on frequency
            var nextPayoutDate = CalculateNextPayoutDate(lastPayoutDate, profile.PayoutFrequency);

            // Calculate available balance
            var completedBookings = await db.Bookings
                .Where(b => b.OwnerId == owner.Id && b.Status == "completed")
                .ToListAsync();

            var totalEarnings = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

            var payouts = await db.Payouts.Where(p => p.OwnerId == owner.Id).ToListAsync();
            var totalPaidOut = payouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
            var pendingPayout = payouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);

            var instantWithdrawals = await db.InstantWithdrawals.Where(w => w.OwnerId == owner.Id).ToListAsync();
            var completedWithdrawals = instantWithdrawals.Where(w => w.Status == "completed").Sum(w => w.Amount);
            var pendingWithdrawals = instantWithdrawals.Where(w => w.Status == "pending" || w.Status == "processing").Sum(w => w.Amount);

            var available = totalEarnings - totalPaidOut - pendingPayout - completedWithdrawals - pendingWithdrawals;

            // Check if payout is due
            var isDueToday = nextPayoutDate.Date <= targetDate && available >= profile.MinimumPayoutAmount;

            scheduledPayouts.Add(new ScheduledPayoutResponse(
                owner.Id,
                $"{owner.FirstName} {owner.LastName}",
                owner.Email ?? "",
                available,
                profile.MinimumPayoutAmount,
                profile.PayoutFrequency,
                lastPayoutDate,
                nextPayoutDate,
                isDueToday,
                profile.PayoutPreference,
                PayoutDetailsDto.Parse(profile.PayoutDetailsJson),
                PayoutDetailsDto.Parse(profile.PayoutDetailsPendingJson),
                profile.PayoutVerificationStatus
            ));
        }

        var duePayouts = scheduledPayouts.Where(p => p.IsDueToday).OrderByDescending(p => p.AvailableBalance).ToList();

        return Results.Ok(new 
        { 
            date = targetDate,
            totalDue = duePayouts.Count,
            totalAmount = duePayouts.Sum(p => p.AvailableBalance),
            payouts = duePayouts
        });
    }

    private static async Task<IResult> ProcessScheduledPayoutsAsync(
        ClaimsPrincipal principal,
        [FromBody] ProcessScheduledPayoutsRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var adminId))
            return Results.Unauthorized();

        var processed = new List<object>();
        var failed = new List<object>();

        foreach (var ownerId in request.OwnerIds)
        {
            try
            {
                var owner = await db.Users
                    .Include(u => u.OwnerProfile)
                    .FirstOrDefaultAsync(u => u.Id == ownerId);

                if (owner?.OwnerProfile == null)
                {
                    failed.Add(new { ownerId, error = "Owner not found" });
                    continue;
                }

                // Calculate available balance
                var completedBookings = await db.Bookings
                    .Where(b => b.OwnerId == ownerId && b.Status == "completed")
                    .ToListAsync();

                var totalEarnings = completedBookings.Sum(b => b.RentalAmount + (b.DriverAmount ?? 0) - (b.PlatformFee ?? 0));

                var payouts = await db.Payouts.Where(p => p.OwnerId == ownerId).ToListAsync();
                var totalPaidOut = payouts.Where(p => p.Status == "completed").Sum(p => p.Amount);
                var pendingPayout = payouts.Where(p => p.Status == "pending" || p.Status == "processing").Sum(p => p.Amount);

                var instantWithdrawals = await db.InstantWithdrawals.Where(w => w.OwnerId == ownerId).ToListAsync();
                var completedWithdrawals = instantWithdrawals.Where(w => w.Status == "completed").Sum(w => w.Amount);
                var pendingWithdrawals = instantWithdrawals.Where(w => w.Status == "pending" || w.Status == "processing").Sum(w => w.Amount);

                var available = totalEarnings - totalPaidOut - pendingPayout - completedWithdrawals - pendingWithdrawals;

                if (available < owner.OwnerProfile.MinimumPayoutAmount)
                {
                    failed.Add(new { ownerId, error = $"Balance {available:F2} below minimum {owner.OwnerProfile.MinimumPayoutAmount:F2}" });
                    continue;
                }

                var payout = new Payout
                {
                    OwnerId = ownerId,
                    Amount = available,
                    Currency = "GHS",
                    Status = "pending",
                    Method = owner.OwnerProfile.PayoutPreference,
                    PayoutDetailsJson = owner.OwnerProfile.PayoutDetailsJson,
                    PeriodStart = payouts.Any() 
                        ? payouts.Where(p => p.Status == "completed").Max(p => p.CompletedAt ?? p.CreatedAt) 
                        : owner.CreatedAt,
                    PeriodEnd = DateTime.UtcNow,
                    Reference = $"PAYOUT-{DateTime.UtcNow:yyyyMMddHHmmss}-{ownerId.ToString()[..8]}"
                };

                db.Payouts.Add(payout);
                processed.Add(new 
                { 
                    ownerId, 
                    ownerName = $"{owner.FirstName} {owner.LastName}",
                    amount = available,
                    payoutId = payout.Id 
                });
            }
            catch (Exception ex)
            {
                failed.Add(new { ownerId, error = ex.Message });
            }
        }

        await db.SaveChangesAsync();

        return Results.Ok(new 
        { 
            message = $"Processed {processed.Count} payouts, {failed.Count} failed",
            processed,
            failed
        });
    }

    private static async Task<IResult> GetPendingInstantWithdrawalsAsync(AppDbContext db)
    {
        var withdrawals = await db.InstantWithdrawals
            .Include(w => w.Owner)
            .ThenInclude(o => o.OwnerProfile)
            .Where(w => w.Status == "pending" || w.Status == "processing")
            .OrderBy(w => w.CreatedAt)
            .Select(w => new
            {
                w.Id,
                w.OwnerId,
                OwnerName = $"{w.Owner.FirstName} {w.Owner.LastName}",
                OwnerEmail = w.Owner.Email,
                w.Amount,
                w.FeeAmount,
                w.NetAmount,
                w.Currency,
                w.Status,
                w.Method,
                w.CreatedAt,
                w.ErrorMessage,
                PayoutDetails = PayoutDetailsDto.Parse(w.PayoutDetailsJson),
                PayoutDetailsPending = w.Owner != null && w.Owner.OwnerProfile != null ? PayoutDetailsDto.Parse(w.Owner.OwnerProfile.PayoutDetailsPendingJson) : null,
                PayoutVerificationStatus = w.Owner != null && w.Owner.OwnerProfile != null ? w.Owner.OwnerProfile.PayoutVerificationStatus : null
            })
            .ToListAsync();

        return Results.Ok(new { total = withdrawals.Count, withdrawals });
    }

    private static DateTime CalculateNextPayoutDate(DateTime lastPayoutDate, string frequency)
    {
        return frequency.ToLowerInvariant() switch
        {
            "daily" => lastPayoutDate.AddDays(1),
            "weekly" => lastPayoutDate.AddDays(7),
            "biweekly" => lastPayoutDate.AddDays(14),
            "monthly" => lastPayoutDate.AddMonths(1),
            _ => lastPayoutDate.AddDays(7) // Default to weekly
        };
    }
}
