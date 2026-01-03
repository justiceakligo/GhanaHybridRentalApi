using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Endpoints;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reviews")
            .WithTags("Reviews")
            .RequireAuthorization();

        // Create review
        group.MapPost("/", CreateReview)
            .WithName("CreateReview")
            .WithDescription("Create a new review for a booking");

        // Get reviews for a target (vehicle, driver, etc.)
        group.MapGet("/target/{targetType}/{targetId:guid}", GetReviewsByTarget)
            .WithName("GetReviewsByTarget")
            .WithDescription("Get all reviews for a specific vehicle or driver")
            .AllowAnonymous();

        // Get reviews by booking
        group.MapGet("/booking/{bookingId:guid}", GetReviewsByBooking)
            .WithName("GetReviewsByBooking")
            .WithDescription("Get reviews for a specific booking");

        // Get user's reviews
        group.MapGet("/my-reviews", GetMyReviews)
            .WithName("GetMyReviews")
            .WithDescription("Get all reviews created by the authenticated user");

        // Update review
        group.MapPut("/{reviewId:guid}", UpdateReview)
            .WithName("UpdateReview")
            .WithDescription("Update an existing review");

        // Delete review
        group.MapDelete("/{reviewId:guid}", DeleteReview)
            .WithName("DeleteReview")
            .WithDescription("Delete a review");

        // Admin endpoints
        var adminGroup = app.MapGroup("/api/v1/admin/reviews")
            .WithTags("Reviews - Admin")
            .RequireAuthorization();

        adminGroup.MapGet("/", GetAllReviews)
            .WithName("GetAllReviews")
            .WithDescription("Get all reviews with filtering");

        adminGroup.MapPost("/{reviewId:guid}/moderate", ModerateReview)
            .WithName("ModerateReview")
            .WithDescription("Moderate a review (approve/reject/flag)");
    }

    private static async Task<IResult> CreateReview(
        CreateReviewDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        // Verify booking exists and belongs to user
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
            .Include(b => b.Driver)
            .FirstOrDefaultAsync(b => b.Id == dto.BookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.RenterId != userId)
            return Results.Forbid();

        // Can only review completed bookings
        if (booking.Status != "completed")
            return Results.BadRequest(new { error = "Can only review completed bookings" });

        // Check if review already exists
        var existingReview = await db.Reviews.FirstOrDefaultAsync(r =>
            r.BookingId == dto.BookingId &&
            r.ReviewerUserId == userId &&
            r.TargetType == dto.TargetType);

        if (existingReview != null)
            return Results.BadRequest(new { error = "Review already exists for this booking and target" });

        // Validate target
        Guid? targetId = null;
        if (dto.TargetType == "vehicle")
        {
            targetId = booking.VehicleId;
        }
        else if (dto.TargetType == "driver" && booking.DriverId.HasValue)
        {
            targetId = booking.DriverId.Value;
        }
        else if (dto.TargetType != "service")
        {
            return Results.BadRequest(new { error = "Invalid target type" });
        }

        var review = new Review
        {
            BookingId = dto.BookingId,
            ReviewerUserId = userId,
            TargetType = dto.TargetType,
            TargetId = targetId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            ModerationStatus = "pending" // Auto-approve or require moderation based on settings
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/reviews/{review.Id}", review);
    }

    private static async Task<IResult> GetReviewsByTarget(
        string targetType,
        Guid targetId,
        AppDbContext db,
        int page = 1,
        int pageSize = 20)
    {
        var query = db.Reviews
            .Include(r => r.ReviewerUser)
            .Where(r =>
                r.TargetType == targetType &&
                r.TargetId == targetId &&
                r.ModerationStatus == "approved" &&
                r.IsVisible);

        var total = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                ReviewerName = r.ReviewerUser != null ? $"{r.ReviewerUser.FirstName} {r.ReviewerUser.LastName}" : "Anonymous"
            })
            .ToListAsync();

        var avgRating = await query.AverageAsync(r => (double?)r.Rating) ?? 0;

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            averageRating = Math.Round(avgRating, 2),
            data = reviews
        });
    }

    private static async Task<IResult> GetReviewsByBooking(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        // Only renter or admin can see booking reviews
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase) && booking.RenterId != userId)
            return Results.Forbid();

        var reviews = await db.Reviews
            .Where(r => r.BookingId == bookingId)
            .OrderBy(r => r.TargetType)
            .ToListAsync();

        return Results.Ok(reviews);
    }

    private static async Task<IResult> GetMyReviews(
        ClaimsPrincipal principal,
        AppDbContext db,
        int page = 1,
        int pageSize = 20)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Reviews
            .Include(r => r.Booking)
            .Where(r => r.ReviewerUserId == userId);

        var total = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = reviews });
    }

    private static async Task<IResult> UpdateReview(
        Guid reviewId,
        UpdateReviewDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null)
            return Results.NotFound(new { error = "Review not found" });

        if (review.ReviewerUserId != userId)
            return Results.Forbid();

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;
        review.ModerationStatus = "pending"; // Re-review after edit

        await db.SaveChangesAsync();

        return Results.Ok(review);
    }

    private static async Task<IResult> DeleteReview(
        Guid reviewId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null)
            return Results.NotFound(new { error = "Review not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase) && review.ReviewerUserId != userId)
            return Results.Forbid();

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Review deleted" });
    }

    private static async Task<IResult> GetAllReviews(
        AppDbContext db,
        HttpContext context,
        string? moderationStatus = null,
        string? targetType = null,
        int page = 1,
        int pageSize = 50)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var query = db.Reviews
            .Include(r => r.ReviewerUser)
            .Include(r => r.Booking)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(moderationStatus))
            query = query.Where(r => r.ModerationStatus == moderationStatus);

        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(r => r.TargetType == targetType);

        var total = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = reviews });
    }

    private static async Task<IResult> ModerateReview(
        Guid reviewId,
        ModerateReviewDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null)
            return Results.NotFound(new { error = "Review not found" });

        var validStatuses = new[] { "approved", "rejected", "flagged" };
        if (!validStatuses.Contains(dto.ModerationStatus))
            return Results.BadRequest(new { error = "Invalid moderation status" });

        review.ModerationStatus = dto.ModerationStatus;
        review.ModeratedByUserId = userId;
        review.ModerationNotes = dto.Notes;
        review.ModeratedAt = DateTime.UtcNow;
        review.IsVisible = dto.ModerationStatus == "approved";

        await db.SaveChangesAsync();

        return Results.Ok(review);
    }
}

public record CreateReviewDto(
    Guid BookingId,
    string TargetType, // "vehicle", "driver", "service"
    int Rating,
    string? Comment
);

public record UpdateReviewDto(
    int Rating,
    string? Comment
);

public record ModerateReviewDto(
    string ModerationStatus, // "approved", "rejected", "flagged"
    string? Notes
);
