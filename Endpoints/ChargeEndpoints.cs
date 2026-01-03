using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class ChargeEndpoints
{
    public static void MapChargeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        // Admin manages charge types
        group.MapGet("/admin/charge-types", ListChargeTypesAsync)
            .RequireAuthorization("AdminOnly");

        group.MapGet("/admin/charge-types/{id:guid}", GetChargeTypeAsync)
            .RequireAuthorization("AdminOnly");

        group.MapPost("/admin/charge-types", CreateChargeTypeAsync)
            .RequireAuthorization("AdminOnly");

        group.MapPut("/admin/charge-types/{id:guid}", UpdateChargeTypeAsync)
            .RequireAuthorization("AdminOnly");

        // Booking charges
        group.MapGet("/bookings/{bookingId:guid}/charges", ListBookingChargesAsync)
            .RequireAuthorization();

        group.MapPost("/bookings/{bookingId:guid}/charges", CreateBookingChargeAsync)
            .RequireAuthorization(); // owner/admin; enforce inside

        group.MapPut("/bookings/{bookingId:guid}/charges/{chargeId:guid}/status", UpdateBookingChargeStatusAsync)
            .RequireAuthorization(); // admin only
    }

    // ==================== Charge Type (Admin) ====================

    private static async Task<IResult> ListChargeTypesAsync(AppDbContext db)
    {
        var types = await db.PostRentalChargeTypes
            .OrderBy(t => t.Name)
            .ToListAsync();

        var response = types.Select(t => new ChargeTypeResponse(
            t.Id,
            t.Code,
            t.Name,
            t.Description,
            t.DefaultAmount,
            t.Currency,
            t.RecipientType,
            t.IsActive,
            t.CreatedAt,
            t.UpdatedAt
        ));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetChargeTypeAsync(Guid id, AppDbContext db)
    {
        var t = await db.PostRentalChargeTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null)
            return Results.NotFound(new { error = "Charge type not found" });

        return Results.Ok(new ChargeTypeResponse(
            t.Id,
            t.Code,
            t.Name,
            t.Description,
            t.DefaultAmount,
            t.Currency,
            t.RecipientType,
            t.IsActive,
            t.CreatedAt,
            t.UpdatedAt
        ));
    }

    private static async Task<IResult> CreateChargeTypeAsync(
        [FromBody] CreateChargeTypeRequest request,
        AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new { error = "Code is required" });

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required" });

        var code = request.Code.Trim().ToLowerInvariant();
        var exists = await db.PostRentalChargeTypes.AnyAsync(t => t.Code == code);
        if (exists)
            return Results.BadRequest(new { error = "Charge type code already exists" });

        var type = new PostRentalChargeType
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description,
            DefaultAmount = request.DefaultAmount,
            Currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "GHS"
                : request.Currency.Trim().ToUpperInvariant(),
            RecipientType = string.IsNullOrWhiteSpace(request.RecipientType)
                ? "owner"
                : request.RecipientType.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.PostRentalChargeTypes.Add(type);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/admin/charge-types/{type.Id}",
            new ChargeTypeResponse(
                type.Id,
                type.Code,
                type.Name,
                type.Description,
                type.DefaultAmount,
                type.Currency,
                type.RecipientType,
                type.IsActive,
                type.CreatedAt,
                type.UpdatedAt
            )
        );
    }

    private static async Task<IResult> UpdateChargeTypeAsync(
        Guid id,
        [FromBody] UpdateChargeTypeRequest request,
        AppDbContext db)
    {
        var type = await db.PostRentalChargeTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
            return Results.NotFound(new { error = "Charge type not found" });

        if (!string.IsNullOrWhiteSpace(request.Name))
            type.Name = request.Name.Trim();

        if (request.Description is not null)
            type.Description = request.Description;

        if (request.DefaultAmount.HasValue)
            type.DefaultAmount = request.DefaultAmount.Value;

        if (!string.IsNullOrWhiteSpace(request.Currency))
            type.Currency = request.Currency.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(request.RecipientType))
            type.RecipientType = request.RecipientType.Trim().ToLowerInvariant();

        if (request.IsActive.HasValue)
            type.IsActive = request.IsActive.Value;

        type.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new ChargeTypeResponse(
            type.Id,
            type.Code,
            type.Name,
            type.Description,
            type.DefaultAmount,
            type.Currency,
            type.RecipientType,
            type.IsActive,
            type.CreatedAt,
            type.UpdatedAt
        ));
    }

    // ==================== Booking Charges ====================

    private static BookingChargeResponse MapBookingChargeToResponse(BookingCharge c)
    {
        List<string> evidence;
        if (string.IsNullOrWhiteSpace(c.EvidencePhotoUrlsJson))
        {
            evidence = new List<string>();
        }
        else
        {
            evidence = JsonSerializer.Deserialize<List<string>>(c.EvidencePhotoUrlsJson) ?? new List<string>();
        }

        return new BookingChargeResponse(
            c.Id,
            c.BookingId,
            c.ChargeTypeId,
            c.ChargeType.Code,
            c.ChargeType.Name,
            c.Amount,
            c.Currency,
            c.Label,
            c.Notes,
            evidence,
            c.Status,
            c.CreatedAt,
            c.CreatedByUserId,
            c.CreatedByUser is null ? null : $"{c.CreatedByUser.FirstName} {c.CreatedByUser.LastName}".Trim(),
            c.SettledAt,
            c.PaymentTransactionId
        );
    }

    private static async Task<IResult> ListBookingChargesAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var allowed = user.Role == "admin" ||
                      booking.OwnerId == userId ||
                      booking.RenterId == userId;

        if (!allowed)
            return Results.Forbid();

        var charges = await db.BookingCharges
            .Include(c => c.ChargeType)
            .Include(c => c.CreatedByUser)
            .Where(c => c.BookingId == bookingId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var response = charges.Select(MapBookingChargeToResponse);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateBookingChargeAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] CreateBookingChargeRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var type = await db.PostRentalChargeTypes
            .FirstOrDefaultAsync(t => t.Id == request.ChargeTypeId && t.IsActive);

        if (type is null)
            return Results.BadRequest(new { error = "Invalid or inactive charge type" });

        if (request.EvidencePhotoUrls is null || request.EvidencePhotoUrls.Count == 0)
            return Results.BadRequest(new { error = "At least one evidence photo is required" });

        var isOwnerOnBooking = string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase) && booking.OwnerId == user.Id;
        var isAdmin = string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase);

        // Only owner on this booking or admin can create; renter cannot
        if (!isOwnerOnBooking && !isAdmin)
            return Results.Forbid();

        // 🔒 Owner cannot set amount; always use admin-defined DefaultAmount
        var amount = type.DefaultAmount;
        if (amount <= 0)
            return Results.BadRequest(new { error = "Charge type amount is not configured" });

        var evidenceJson = JsonSerializer.Serialize(request.EvidencePhotoUrls);

        var status = isAdmin ? "approved" : "pending_review";

        var charge = new BookingCharge
        {
            BookingId = booking.Id,
            ChargeTypeId = type.Id,
            Amount = amount,
            Currency = type.Currency,
            Label = string.IsNullOrWhiteSpace(request.Label) ? type.Name : request.Label,
            Notes = request.Notes,
            EvidencePhotoUrlsJson = evidenceJson,
            Status = status,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.BookingCharges.Add(charge);
        await db.SaveChangesAsync();

        // Need to load the navigation properties for the response
        await db.Entry(charge).Reference(c => c.ChargeType).LoadAsync();
        await db.Entry(charge).Reference(c => c.CreatedByUser).LoadAsync();

        return Results.Created(
            $"/api/v1/bookings/{bookingId}/charges/{charge.Id}",
            MapBookingChargeToResponse(charge)
        );
    }

    private static async Task<IResult> UpdateBookingChargeStatusAsync(
        Guid bookingId,
        Guid chargeId,
        ClaimsPrincipal principal,
        [FromBody] UpdateBookingChargeStatusRequest request,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var charge = await db.BookingCharges
            .Include(c => c.ChargeType)
            .Include(c => c.CreatedByUser)
            .FirstOrDefaultAsync(c => c.Id == chargeId && c.BookingId == bookingId);

        if (charge is null)
            return Results.NotFound(new { error = "Charge not found" });

        var isAdmin = string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin)
        {
            // 🔒 Owners cannot change status at all (no approve/paid/waive); they can only create.
            return Results.Forbid();
        }

        var newStatus = request.Status?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(newStatus))
            return Results.BadRequest(new { error = "Status is required" });

        var allowedStatuses = new[] { "pending_review", "approved", "rejected", "paid", "waived" };
        if (!allowedStatuses.Contains(newStatus))
            return Results.BadRequest(new { error = "Invalid status" });

        switch (newStatus)
        {
            case "approved":
                charge.Status = "approved";
                charge.SettledAt = null;
                charge.PaymentTransactionId = null;
                break;

            case "rejected":
                charge.Status = "rejected";
                charge.SettledAt = DateTime.UtcNow;
                charge.PaymentTransactionId = null;
                break;

            case "paid":
                if (request.PaymentTransactionId is null)
                    return Results.BadRequest(new { error = "PaymentTransactionId is required when marking paid" });

                var txExists = await db.PaymentTransactions
                    .AnyAsync(t => t.Id == request.PaymentTransactionId.Value);

                if (!txExists)
                    return Results.BadRequest(new { error = "Payment transaction not found" });

                charge.Status = "paid";
                charge.SettledAt = DateTime.UtcNow;
                charge.PaymentTransactionId = request.PaymentTransactionId;
                break;

            case "waived":
                charge.Status = "waived";
                charge.SettledAt = DateTime.UtcNow;
                charge.PaymentTransactionId = null;
                break;

            case "pending_review":
                charge.Status = "pending_review";
                charge.SettledAt = null;
                charge.PaymentTransactionId = null;
                break;
        }

        await db.SaveChangesAsync();
        return Results.Ok(MapBookingChargeToResponse(charge));
    }
}
