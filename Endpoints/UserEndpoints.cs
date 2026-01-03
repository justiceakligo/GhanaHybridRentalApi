using System.Security.Claims;
using GhanaHybridRentalApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/me", GetMeAsync)
            .RequireAuthorization();
        
        // Admin endpoint to get renter details
        app.MapGet("/api/v1/admin/renters/{userId:guid}", GetRenterDetailsAsync)
            .RequireAuthorization("AdminOnly");
        
        // Admin endpoint to list all users is defined in AdminEndpoints.cs to avoid route conflicts (duplicate removed)
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users
            .Include(u => u.OwnerProfile)
            .Include(u => u.RenterProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Results.NotFound();

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.Phone,
            user.Role,
            user.Status,
            user.PhoneVerified,
            OwnerProfile = user.OwnerProfile,
            RenterProfile = user.RenterProfile
        });
    }

    private static async Task<IResult> GetRenterDetailsAsync(
        Guid userId,
        AppDbContext db)
    {
        var user = await db.Users
            .Include(u => u.RenterProfile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == "renter");

        if (user is null)
            return Results.NotFound(new { error = "Renter not found" });

        var bookingsCount = await db.Bookings.CountAsync(b => b.RenterId == userId);
        var totalSpent = await db.PaymentTransactions
            .Where(t => t.UserId == userId && t.Type == "payment" && t.Status == "completed")
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.Phone,
            user.FirstName,
            user.LastName,
            user.Role,
            user.Status,
            user.PhoneVerified,
            user.CreatedAt,
            RenterProfile = user.RenterProfile,
            Statistics = new
            {
                TotalBookings = bookingsCount,
                TotalSpent = totalSpent
            }
        });
    }

    private static async Task<IResult> GetAllUsersAsync(
        AppDbContext db,
        string? role = null,
        string? status = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = db.Users
            .Include(u => u.OwnerProfile)
            .Include(u => u.RenterProfile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = users.Select(u => new
            {
                u.Id,
                u.Email,
                u.Phone,
                u.FirstName,
                u.LastName,
                u.Role,
                u.Status,
                u.PhoneVerified,
                u.CreatedAt,
                OwnerProfile = u.OwnerProfile,
                RenterProfile = u.RenterProfile
            })
        });
    }
}
