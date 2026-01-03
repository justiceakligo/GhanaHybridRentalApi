using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        // Create report
        group.MapPost("/", CreateReport)
            .WithName("CreateReport")
            .WithDescription("Create a new report");

        // Get my reports
        group.MapGet("/my-reports", GetMyReports)
            .WithName("GetMyReports")
            .WithDescription("Get all reports created by the authenticated user");

        // Admin endpoints
        var adminGroup = app.MapGroup("/api/v1/admin/reports")
            .WithTags("Reports - Admin")
            .RequireAuthorization();

        adminGroup.MapGet("/", GetAllReports)
            .WithName("GetAllReports")
            .WithDescription("Get all reports with filtering");

        adminGroup.MapGet("/{reportId:guid}", GetReport)
            .WithName("GetReport")
            .WithDescription("Get a specific report");

        adminGroup.MapPost("/{reportId:guid}/review", ReviewReport)
            .WithName("ReviewReport")
            .WithDescription("Review a report and take action");

        adminGroup.MapPost("/{reportId:guid}/action", TakeAction)
            .WithName("TakeAction")
            .WithDescription("Take action on reported user/content (warn/suspend/ban)");
    }

    private static async Task<IResult> CreateReport(
        CreateReportDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var validTargetTypes = new[] { "user", "vehicle", "booking", "review" };
        if (!validTargetTypes.Contains(dto.TargetType))
            return Results.BadRequest(new { error = "Invalid target type" });

        var validReasons = new[] { "inappropriate_content", "fraud", "harassment", "spam", "safety_concern", "other" };
        if (!validReasons.Contains(dto.Reason))
            return Results.BadRequest(new { error = "Invalid reason" });

        // Verify target exists based on type
        var targetExists = dto.TargetType switch
        {
            "user" => await db.Users.AnyAsync(u => u.Id == dto.TargetId),
            "vehicle" => await db.Vehicles.AnyAsync(v => v.Id == dto.TargetId),
            "booking" => await db.Bookings.AnyAsync(b => b.Id == dto.TargetId),
            "review" => await db.Reviews.AnyAsync(r => r.Id == dto.TargetId),
            _ => false
        };

        if (!targetExists)
            return Results.BadRequest(new { error = $"{dto.TargetType} not found" });

        var report = new Report
        {
            ReporterUserId = userId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            Reason = dto.Reason,
            Description = dto.Description,
            Status = "pending"
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/reports/{report.Id}", report);
    }

    private static async Task<IResult> GetMyReports(
        ClaimsPrincipal principal,
        AppDbContext db,
        int page = 1,
        int pageSize = 20)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var query = db.Reports.Where(r => r.ReporterUserId == userId);

        var total = await query.CountAsync();
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, data = reports });
    }

    private static async Task<IResult> GetAllReports(
        AppDbContext db,
        HttpContext context,
        string? status = null,
        string? targetType = null,
        string? reason = null,
        int page = 1,
        int pageSize = 50)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var query = db.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(r => r.TargetType == targetType);

        if (!string.IsNullOrWhiteSpace(reason))
            query = query.Where(r => r.Reason == reason);

        var total = await query.CountAsync();
        var reports = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Enhance reports with target entity data
        var enrichedReports = new List<object>();
        foreach (var report in reports)
        {
            object? targetEntity = null;
            string? targetName = null;

            // Fetch target entity based on type
            switch (report.TargetType)
            {
                case "user":
                    var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Id == report.TargetId);
                    if (targetUser != null)
                    {
                        targetName = $"{targetUser.FirstName} {targetUser.LastName}";
                        targetEntity = new
                        {
                            id = targetUser.Id,
                            firstName = targetUser.FirstName,
                            lastName = targetUser.LastName,
                            email = targetUser.Email,
                            phone = targetUser.Phone,
                            role = targetUser.Role,
                            status = targetUser.Status
                        };
                    }
                    break;

                case "vehicle":
                    var vehicle = await db.Vehicles
                        .Include(v => v.Owner)
                        .FirstOrDefaultAsync(v => v.Id == report.TargetId);
                    if (vehicle != null)
                    {
                        targetName = $"{vehicle.Year} {vehicle.Make} {vehicle.Model}";
                        targetEntity = new
                        {
                            id = vehicle.Id,
                            make = vehicle.Make,
                            model = vehicle.Model,
                            year = vehicle.Year,
                            plateNumber = vehicle.PlateNumber,
                            status = vehicle.Status,
                            owner = vehicle.Owner != null ? new
                            {
                                id = vehicle.Owner.Id,
                                firstName = vehicle.Owner.FirstName,
                                lastName = vehicle.Owner.LastName,
                                email = vehicle.Owner.Email
                            } : null
                        };
                    }
                    break;

                case "booking":
                    var booking = await db.Bookings
                        .Include(b => b.Vehicle)
                        .Include(b => b.Renter)
                        .FirstOrDefaultAsync(b => b.Id == report.TargetId);
                    if (booking != null)
                    {
                        targetName = $"Booking {booking.BookingReference}";
                        targetEntity = new
                        {
                            id = booking.Id,
                            bookingReference = booking.BookingReference,
                            status = booking.Status,
                            pickupDateTime = booking.PickupDateTime,
                            returnDateTime = booking.ReturnDateTime,
                            vehicle = booking.Vehicle != null ? new
                            {
                                make = booking.Vehicle.Make,
                                model = booking.Vehicle.Model,
                                year = booking.Vehicle.Year
                            } : null,
                            renter = booking.Renter != null ? new
                            {
                                firstName = booking.Renter.FirstName,
                                lastName = booking.Renter.LastName,
                                email = booking.Renter.Email
                            } : null
                        };
                    }
                    break;

                case "review":
                    var review = await db.Reviews
                        .Include(r => r.ReviewerUser)
                        .FirstOrDefaultAsync(r => r.Id == report.TargetId);
                    if (review != null)
                    {
                        targetName = $"Review by {review.ReviewerUser?.FirstName ?? "Unknown"}";
                        targetEntity = new
                        {
                            id = review.Id,
                            rating = review.Rating,
                            comment = review.Comment,
                            isVisible = review.IsVisible,
                            moderationStatus = review.ModerationStatus,
                            reviewer = review.ReviewerUser != null ? new
                            {
                                firstName = review.ReviewerUser.FirstName,
                                lastName = review.ReviewerUser.LastName,
                                email = review.ReviewerUser.Email
                            } : null
                        };
                    }
                    break;
            }

            // Calculate priority based on reason
            var priority = report.Reason switch
            {
                "fraud" => "high",
                "harassment" => "high",
                "safety_concern" => "high",
                "inappropriate_content" => "medium",
                "spam" => "low",
                _ => "low"
            };

            enrichedReports.Add(new
            {
                id = report.Id,
                targetType = report.TargetType,
                targetId = report.TargetId,
                reason = report.Reason,
                description = report.Description,
                status = report.Status,
                createdAt = report.CreatedAt,
                updatedAt = report.UpdatedAt,
                
                // Reporter information
                reporterId = report.ReporterUserId,
                reporter = report.ReporterUser != null ? new
                {
                    id = report.ReporterUser.Id,
                    firstName = report.ReporterUser.FirstName,
                    lastName = report.ReporterUser.LastName,
                    email = report.ReporterUser.Email,
                    role = report.ReporterUser.Role
                } : null,
                reporterName = report.ReporterUser != null 
                    ? $"{report.ReporterUser.FirstName} {report.ReporterUser.LastName}" 
                    : "Unknown",
                
                // Target information
                targetName,
                targetEntity,
                
                // Additional fields
                priority,
                actionTaken = report.ActionTaken,
                adminNotes = report.AdminNotes,
                reviewedAt = report.ReviewedAt,
                reviewedBy = report.ReviewedByUser != null ? new
                {
                    id = report.ReviewedByUser.Id,
                    firstName = report.ReviewedByUser.FirstName,
                    lastName = report.ReviewedByUser.LastName
                } : null
            });
        }

        return Results.Ok(new { total, page, pageSize, data = enrichedReports });
    }

    private static async Task<IResult> GetReport(
        Guid reportId,
        AppDbContext db,
        HttpContext context)
    {
        var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var report = await db.Reports
            .Include(r => r.ReporterUser)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return Results.NotFound(new { error = "Report not found" });

        // Fetch the actual target entity for context
        object? targetEntity = report.TargetType switch
        {
            "user" => await db.Users.FirstOrDefaultAsync(u => u.Id == report.TargetId),
            "vehicle" => await db.Vehicles.FirstOrDefaultAsync(v => v.Id == report.TargetId),
            "booking" => await db.Bookings.FirstOrDefaultAsync(b => b.Id == report.TargetId),
            "review" => await db.Reviews.FirstOrDefaultAsync(r => r.Id == report.TargetId),
            _ => null
        };

        return Results.Ok(new { report, targetEntity });
    }

    private static async Task<IResult> ReviewReport(
        Guid reportId,
        ReviewReportDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null)
            return Results.NotFound(new { error = "Report not found" });

        var validStatuses = new[] { "under_review", "resolved", "dismissed" };
        if (!validStatuses.Contains(dto.Status))
            return Results.BadRequest(new { error = "Invalid status" });

        report.Status = dto.Status;
        report.ReviewedByUserId = userId;
        report.AdminNotes = dto.AdminNotes;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(report);
    }

    private static async Task<IResult> TakeAction(
        Guid reportId,
        TakeActionDto dto,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (!string.Equals(user?.Role, "admin", StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null)
            return Results.NotFound(new { error = "Report not found" });

        var validActions = new[] { "warning", "suspension", "ban", "content_removed" };
        if (!validActions.Contains(dto.Action))
            return Results.BadRequest(new { error = "Invalid action" });

        // Take action based on target type
        switch (report.TargetType)
        {
            case "user":
                var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Id == report.TargetId);
                if (targetUser != null)
                {
                    targetUser.Status = dto.Action switch
                    {
                        "warning" => targetUser.Status, // Just log warning, don't change status
                        "suspension" => "suspended",
                        "ban" => "banned",
                        _ => targetUser.Status
                    };
                }
                break;

            case "vehicle":
                var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == report.TargetId);
                if (vehicle != null && dto.Action == "content_removed")
                {
                    vehicle.Status = "removed";
                }
                break;

            case "review":
                var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == report.TargetId);
                if (review != null && dto.Action == "content_removed")
                {
                    review.IsVisible = false;
                    review.ModerationStatus = "rejected";
                }
                break;
        }

        report.ActionTaken = dto.Action;
        report.Status = "resolved";
        report.ReviewedByUserId = userId;
        report.AdminNotes = dto.Notes;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            report.Id,
            report.Status,
            report.ActionTaken,
            message = $"Action '{dto.Action}' taken successfully"
        });
    }
}

public record CreateReportDto(
    string TargetType, // "user", "vehicle", "booking", "review"
    Guid TargetId,
    string Reason, // "inappropriate_content", "fraud", "harassment", "spam", "safety_concern", "other"
    string? Description
);

public record ReviewReportDto(
    string Status, // "under_review", "resolved", "dismissed"
    string? AdminNotes
);

public record TakeActionDto(
    string Action, // "warning", "suspension", "ban", "content_removed"
    string? Notes
);
