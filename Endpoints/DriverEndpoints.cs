using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class DriverEndpoints
{
    public static void MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        // Driver profile management
        app.MapGet("/api/v1/driver/profile", GetDriverProfileAsync)
            .RequireAuthorization();

        app.MapPost("/api/v1/driver/profile", CreateDriverProfileAsync)
            .RequireAuthorization();

        app.MapPut("/api/v1/driver/profile", UpdateDriverProfileAsync)
            .RequireAuthorization();

        // Admin driver management
        app.MapGet("/api/v1/admin/drivers", GetDriversAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPut("/api/v1/admin/drivers/{driverId:guid}/verification", UpdateDriverVerificationAsync)
            .RequireAuthorization("AdminOnly");

        // Booking driver assignment
        app.MapPut("/api/v1/bookings/{bookingId:guid}/assign-driver", AssignDriverToBookingAsync)
            .RequireAuthorization();

        // Driver availability
        app.MapPut("/api/v1/driver/availability", UpdateDriverAvailabilityAsync)
            .RequireAuthorization();

        // Driver bookings
        app.MapGet("/api/v1/driver/bookings", GetDriverBookingsAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetDriverProfileAsync(
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (profile is null)
            return Results.NotFound(new { error = "Driver profile not found" });

        return Results.Ok(profile);
    }

    private static async Task<IResult> CreateDriverProfileAsync(
        ClaimsPrincipal principal,
        [FromBody] CreateDriverProfileRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        var existingProfile = await db.DriverProfiles.AnyAsync(d => d.UserId == userId);
        if (existingProfile)
            return Results.BadRequest(new { error = "Driver profile already exists" });

        var validTypes = new[] { "independent", "owner_employed", "platform" };
        if (!validTypes.Contains(request.DriverType.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid driver type" });

        if (request.DriverType.ToLowerInvariant() == "owner_employed" && !request.OwnerEmployerId.HasValue)
            return Results.BadRequest(new { error = "Owner employer ID required for owner_employed drivers" });

        var profile = new DriverProfile
        {
            UserId = userId,
            FullName = request.FullName,
            LicenseNumber = request.LicenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,
            DriverType = request.DriverType.ToLowerInvariant(),
            OwnerEmployerId = request.OwnerEmployerId,
            VerificationStatus = "pending",
            Available = true
        };

        db.DriverProfiles.Add(profile);

        // Update user role if not already a driver
        if (!string.Equals(user.Role, "driver", StringComparison.OrdinalIgnoreCase))
            user.Role = "driver";

        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/driver/profile", profile);
    }

    private static async Task<IResult> UpdateDriverProfileAsync(
        ClaimsPrincipal principal,
        [FromBody] UpdateDriverProfileRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
        if (profile is null)
            return Results.NotFound(new { error = "Driver profile not found" });

        if (!string.IsNullOrWhiteSpace(request.FullName))
            profile.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
            profile.LicenseNumber = request.LicenseNumber;

        if (request.LicenseExpiryDate.HasValue)
            profile.LicenseExpiryDate = request.LicenseExpiryDate.Value;

        if (request.Available.HasValue)
            profile.Available = request.Available.Value;

        await db.SaveChangesAsync();

        return Results.Ok(profile);
    }

    private static async Task<IResult> GetDriversAsync(
        AppDbContext db,
        [FromQuery] string? verificationStatus,
        [FromQuery] bool? available,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.DriverProfiles
            .Include(d => d.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(verificationStatus))
            query = query.Where(d => d.VerificationStatus == verificationStatus.ToLowerInvariant());

        if (available.HasValue)
            query = query.Where(d => d.Available == available.Value);

        var total = await query.CountAsync();
        var drivers = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = drivers
        });
    }

    private static async Task<IResult> UpdateDriverVerificationAsync(
        Guid driverId,
        [FromBody] UpdateDriverVerificationRequest request,
        AppDbContext db)
    {
        var profile = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == driverId);
        if (profile is null)
            return Results.NotFound(new { error = "Driver profile not found" });

        var validStatuses = new[] { "unverified", "pending", "verified", "rejected" };
        if (!validStatuses.Contains(request.VerificationStatus.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid verification status" });

        profile.VerificationStatus = request.VerificationStatus.ToLowerInvariant();
        await db.SaveChangesAsync();

        return Results.Ok(new { profile.UserId, profile.VerificationStatus });
    }

    private static async Task<IResult> AssignDriverToBookingAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] AssignDriverRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check if user is the owner or admin
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var isAuthorized = string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase) || booking.OwnerId == userId;

        if (!isAuthorized)
            return Results.Forbid();

        if (!booking.WithDriver)
            return Results.BadRequest(new { error = "Booking is not configured for driver assignment" });

        var driver = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == request.DriverId);
        if (driver is null)
            return Results.BadRequest(new { error = "Driver not found" });

        if (driver.VerificationStatus != "verified")
            return Results.BadRequest(new { error = "Driver is not verified" });

        if (!driver.Available)
            return Results.BadRequest(new { error = "Driver is not available" });

        booking.DriverId = request.DriverId;
        await db.SaveChangesAsync();

        return Results.Ok(new { booking.Id, booking.DriverId });
    }

    private static async Task<IResult> UpdateDriverAvailabilityAsync(
        ClaimsPrincipal principal,
        [FromBody] UpdateDriverProfileRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var profile = await db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
        if (profile is null)
            return Results.NotFound(new { error = "Driver profile not found" });

        if (request.Available.HasValue)
            profile.Available = request.Available.Value;

        await db.SaveChangesAsync();

        return Results.Ok(new { profile.UserId, profile.Available });
    }

    private static async Task<IResult> GetDriverBookingsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        [FromQuery] string? status)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Bookings
            .Where(b => b.DriverId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status.ToLowerInvariant());

        var bookings = await query
            .OrderByDescending(b => b.PickupDateTime)
            .ToListAsync();

        return Results.Ok(bookings);
    }
}
