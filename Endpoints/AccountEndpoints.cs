using System.Security.Claims;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/account/deactivate", DeactivateAccountAsync)
            .RequireAuthorization();

        app.MapDelete("/api/v1/account", DeleteAccountAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> DeactivateAccountAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        user.Status = "suspended";
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Account deactivated" });
    }

    private static async Task<IResult> DeleteAccountAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        // Prevent deletion if user has active bookings
        var hasActiveBookings = await db.Bookings.AnyAsync(b => (b.OwnerId == userId || b.RenterId == userId) && b.Status != "cancelled" && b.Status != "completed");
        if (hasActiveBookings)
            return Results.BadRequest(new { error = "Cannot delete account with active or pending bookings" });
        // Prevent deletion if there are pending or processing payouts
        var hasPendingPayouts = await db.Payouts.AnyAsync(p => p.OwnerId == userId && (p.Status == "pending" || p.Status == "processing"));
        if (hasPendingPayouts)
            return Results.BadRequest(new { error = "Cannot delete account with pending or processing payouts" });
        // Delete owner-specific data
        if (string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase))
        {
            var vehicles = await db.Vehicles.Where(v => v.OwnerId == userId && v.DeletedAt == null).ToListAsync();
            db.Vehicles.RemoveRange(vehicles);

            var payouts = await db.Payouts.Where(p => p.OwnerId == userId).ToListAsync();
            db.Payouts.RemoveRange(payouts);

            var ownerProfile = await db.OwnerProfiles.FirstOrDefaultAsync(op => op.UserId == userId);
            if (ownerProfile != null) db.OwnerProfiles.Remove(ownerProfile);
        }

        // Delete renter-specific data
        if (string.Equals(user.Role, "renter", StringComparison.OrdinalIgnoreCase))
        {
            var renterProfile = await db.RenterProfiles.FirstOrDefaultAsync(rp => rp.UserId == userId);
            if (renterProfile != null) db.RenterProfiles.Remove(renterProfile);

            var reviews = await db.Reviews.Where(r => r.ReviewerUserId == userId).ToListAsync();
            if (reviews.Any()) db.Reviews.RemoveRange(reviews);
        }

        // Delete bookings where user was renter or owner but are completed/cancelled
        var bookings = await db.Bookings.Where(b => b.RenterId == userId || b.OwnerId == userId).ToListAsync();
        if (bookings.Any()) db.Bookings.RemoveRange(bookings);

        // Finally delete user
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "Account and related data deleted" });
    }
}
