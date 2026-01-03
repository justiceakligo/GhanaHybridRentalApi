using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Services;

namespace GhanaHybridRentalApi.Endpoints;

public static class DepositRefundEndpoints
{
    public static void MapDepositRefundEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/refunds").RequireAuthorization();

        // Admin endpoints
        group.MapGet("/admin/pending", GetPendingRefundsAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapGet("/admin/overdue", GetOverdueRefundsAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapGet("/admin/{id:guid}", GetRefundDetailsAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapPost("/admin/create", CreateDepositRefundAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapPost("/admin/{id:guid}/process", ProcessRefundAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapPost("/admin/{id:guid}/cancel", CancelRefundAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));

        group.MapGet("/admin/audit/{refundId:guid}", GetRefundAuditLogAsync)
            .RequireAuthorization(policy => policy.RequireRole("admin"));
    }

    private static async Task<IResult> GetPendingRefundsAsync(AppDbContext db)
    {
        var refunds = await db.DepositRefunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Vehicle)
            .Where(r => r.Status == "pending")
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var response = refunds.Select(r => MapToResponse(r)).ToList();

        return Results.Ok(new { total = response.Count, refunds = response });
    }

    private static async Task<IResult> GetOverdueRefundsAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var refunds = await db.DepositRefunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Vehicle)
            .Where(r => r.Status == "pending" && r.DueDate < now)
            .OrderBy(r => r.DueDate)
            .ToListAsync();

        var response = refunds.Select(r => MapToResponse(r)).ToList();

        return Results.Ok(new { total = response.Count, refunds = response });
    }

    private static async Task<IResult> GetRefundDetailsAsync(Guid id, AppDbContext db)
    {
        var refund = await db.DepositRefunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .Include(r => r.Booking)
                .ThenInclude(b => b.Vehicle)
            .Include(r => r.ProcessedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (refund == null)
            return Results.NotFound(new { error = "Refund not found" });

        return Results.Ok(MapToResponse(refund));
    }

    private static async Task<IResult> CreateDepositRefundAsync(
        ClaimsPrincipal principal,
        [FromBody] CreateDepositRefundRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Renter)
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        if (booking.Status != "completed")
            return Results.BadRequest(new { error = "Can only create refund for completed bookings" });

        if (booking.DepositAmount <= 0)
            return Results.BadRequest(new { error = "No deposit to refund" });

        // Check if refund already exists
        var existingRefund = await db.DepositRefunds
            .FirstOrDefaultAsync(r => r.BookingId == booking.Id);

        if (existingRefund != null)
            return Results.BadRequest(new { error = "Refund already exists for this booking" });

        var refund = new DepositRefund
        {
            BookingId = booking.Id,
            Amount = booking.DepositAmount,
            Currency = booking.Currency,
            PaymentMethod = booking.PaymentMethod,
            Status = "pending",
            DueDate = booking.ReturnDateTime.AddDays(2),
            Notes = request.Notes,
            Reference = $"REF-{booking.BookingReference}-{DateTime.UtcNow:yyyyMMddHHmmss}"
        };

        db.DepositRefunds.Add(refund);

        // Create audit log
        var auditLog = new RefundAuditLog
        {
            DepositRefundId = refund.Id,
            Action = "created",
            OldStatus = "",
            NewStatus = "pending",
            PerformedByUserId = userId,
            Notes = "Deposit refund created by admin"
        };

        db.RefundAuditLogs.Add(auditLog);

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Deposit refund created successfully", refundId = refund.Id });
    }

    private static async Task<IResult> ProcessRefundAsync(
        Guid id,
        ClaimsPrincipal principal,
        [FromBody] ProcessRefundRequest request,
        AppDbContext db,
        IPaystackPaymentService paystack,
        INotificationService notifications)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var refund = await db.DepositRefunds
            .Include(r => r.Booking)
                .ThenInclude(b => b.Renter)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (refund == null)
            return Results.NotFound(new { error = "Refund not found" });

        if (refund.Status != "pending")
            return Results.BadRequest(new { error = $"Cannot process refund with status: {refund.Status}" });

        var oldStatus = refund.Status;

        // Mark as processing
        refund.Status = "processing";
        refund.ProcessedByUserId = userId;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.Notes = string.IsNullOrWhiteSpace(refund.Notes) 
            ? request.Notes 
            : $"{refund.Notes}\n\n[Admin Processing] {request.Notes}";

        // Create audit log
        var auditLog = new RefundAuditLog
        {
            DepositRefundId = refund.Id,
            Action = "processing",
            OldStatus = oldStatus,
            NewStatus = "processing",
            PerformedByUserId = userId,
            Notes = request.Notes
        };

        db.RefundAuditLogs.Add(auditLog);

        await db.SaveChangesAsync();

        // Try to find payment transaction reference for this booking
        string? paymentReference = null;
        var paymentTxn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.BookingId == refund.BookingId && t.Type == "payment" && t.Status == "completed");
        if (paymentTxn != null)
            paymentReference = paymentTxn.Reference ?? paymentTxn.ExternalTransactionId;

        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            // No linked transaction found; fail and ask admin to refund manually
            refund.Status = "failed";
            refund.ErrorMessage = "No completed payment transaction found for booking to refund";

            var failLog = new RefundAuditLog
            {
                DepositRefundId = refund.Id,
                Action = "failed",
                OldStatus = "processing",
                NewStatus = "failed",
                PerformedByUserId = userId,
                Notes = refund.ErrorMessage
            };
            db.RefundAuditLogs.Add(failLog);
            await db.SaveChangesAsync();

            // Notify admin/renter about failure
            if (refund.Booking?.Renter != null)
            {
                var job = new Models.NotificationJob
                {
                    BookingId = refund.BookingId,
                    TargetUserId = refund.Booking.Renter.Id,
                    ChannelsJson = JsonSerializer.Serialize(new[]{"inapp","email","whatsapp"}),
                    Subject = "Refund failed",
                    Message = $"Refund for booking {refund.Booking.BookingReference} could not be processed automatically: {refund.ErrorMessage}",
                    SendImmediately = true
                };
                await notifications.CreateNotificationJobAsync(job);
            }

            return Results.BadRequest(new { error = refund.ErrorMessage });
        }

        // Call Paystack refund API
        var refundResult = await paystack.RefundTransactionAsync(paymentReference, refund.Amount);

        if (refundResult.Success)
        {
            refund.Status = "completed";
            refund.CompletedAt = DateTime.UtcNow;
            refund.ExternalRefundId = refundResult.RefundId;

            var completeLog = new RefundAuditLog
            {
                DepositRefundId = refund.Id,
                Action = "completed",
                OldStatus = "processing",
                NewStatus = "completed",
                PerformedByUserId = userId,
                Notes = request.Notes
            };
            db.RefundAuditLogs.Add(completeLog);

            await db.SaveChangesAsync();

            // Notify renter of success
            if (refund.Booking?.Renter != null)
            {
                var job = new Models.NotificationJob
                {
                    BookingId = refund.BookingId,
                    TargetUserId = refund.Booking.Renter.Id,
                    ChannelsJson = JsonSerializer.Serialize(new[]{"inapp","email","whatsapp"}),
                    Subject = "Refund processed",
                    Message = $"Your refund for booking {refund.Booking.BookingReference} has been processed. Amount: {refund.Currency} {refund.Amount:F2}",
                    SendImmediately = true
                };
                await notifications.CreateNotificationJobAsync(job);
            }

            return Results.Ok(new { message = "Refund completed", refundId = refund.Id });
        }
        else
        {
            refund.Status = "failed";
            refund.ErrorMessage = refundResult.Message ?? "Refund failed";

            var failLog = new RefundAuditLog
            {
                DepositRefundId = refund.Id,
                Action = "failed",
                OldStatus = "processing",
                NewStatus = "failed",
                PerformedByUserId = userId,
                Notes = refund.ErrorMessage
            };
            db.RefundAuditLogs.Add(failLog);
            await db.SaveChangesAsync();

            // Notify renter/admin of failure
            if (refund.Booking?.Renter != null)
            {
                var job = new Models.NotificationJob
                {
                    BookingId = refund.BookingId,
                    TargetUserId = refund.Booking.Renter.Id,
                    ChannelsJson = JsonSerializer.Serialize(new[]{"inapp","email","whatsapp"}),
                    Subject = "Refund failed",
                    Message = $"Automatic refund for booking {refund.Booking.BookingReference} failed: {refund.ErrorMessage}. Please contact support.",
                    SendImmediately = true
                };
                await notifications.CreateNotificationJobAsync(job);
            }

            return Results.BadRequest(new { error = refund.ErrorMessage });
        }
    }

    private static async Task<IResult> CancelRefundAsync(
        Guid id,
        ClaimsPrincipal principal,
        [FromBody] ProcessRefundRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var refund = await db.DepositRefunds.FirstOrDefaultAsync(r => r.Id == id);

        if (refund == null)
            return Results.NotFound(new { error = "Refund not found" });

        if (refund.Status == "completed")
            return Results.BadRequest(new { error = "Cannot cancel completed refund" });

        var oldStatus = refund.Status;
        refund.Status = "cancelled";
        refund.ProcessedByUserId = userId;
        refund.ProcessedAt = DateTime.UtcNow;
        refund.Notes = string.IsNullOrWhiteSpace(refund.Notes) 
            ? request.Notes 
            : $"{refund.Notes}\n\n[Cancelled] {request.Notes}";

        // Create audit log
        var auditLog = new RefundAuditLog
        {
            DepositRefundId = refund.Id,
            Action = "cancelled",
            OldStatus = oldStatus,
            NewStatus = "cancelled",
            PerformedByUserId = userId,
            Notes = request.Notes
        };

        db.RefundAuditLogs.Add(auditLog);

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Refund cancelled successfully" });
    }

    private static async Task<IResult> GetRefundAuditLogAsync(Guid refundId, AppDbContext db)
    {
        var logs = await db.RefundAuditLogs
            .Include(l => l.PerformedByUser)
            .Where(l => l.DepositRefundId == refundId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.Action,
                l.OldStatus,
                l.NewStatus,
                l.Notes,
                l.CreatedAt,
                PerformedBy = l.PerformedByUser != null 
                    ? $"{l.PerformedByUser.FirstName} {l.PerformedByUser.LastName}" 
                    : "System"
            })
            .ToListAsync();

        return Results.Ok(logs);
    }

    private static DepositRefundResponse MapToResponse(DepositRefund refund)
    {
        var now = DateTime.UtcNow;
        var isOverdue = refund.Status == "pending" && refund.DueDate.HasValue && refund.DueDate < now;
        var daysOverdue = isOverdue ? (now - refund.DueDate.GetValueOrDefault(now)).Days : 0;

        return new DepositRefundResponse(
            refund.Id,
            refund.BookingId,
            refund.Booking?.BookingReference ?? "",
            refund.Amount,
            refund.Currency,
            refund.Status,
            refund.PaymentMethod,
            refund.CreatedAt,
            refund.DueDate,
            refund.ProcessedAt,
            refund.CompletedAt,
            isOverdue,
            daysOverdue,
            refund.ErrorMessage,
            refund.Notes,
            refund.Booking?.Renter != null 
                ? $"{refund.Booking.Renter.FirstName} {refund.Booking.Renter.LastName}" 
                : refund.Booking?.GuestFirstName != null 
                    ? $"{refund.Booking.GuestFirstName} {refund.Booking.GuestLastName}" 
                    : "Unknown",
            refund.Booking?.Renter?.Email ?? refund.Booking?.GuestEmail ?? "",
            refund.Booking?.Vehicle != null 
                ? $"{refund.Booking.Vehicle.Make} {refund.Booking.Vehicle.Model} ({refund.Booking.Vehicle.PlateNumber})" 
                : ""
        );
    }
}
