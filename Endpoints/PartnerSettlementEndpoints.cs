using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class PartnerSettlementEndpoints
{
    public static void MapPartnerSettlementEndpoints(this IEndpointRouteBuilder app)
    {
        // Admin endpoints for managing partner settlements
        app.MapGet("/api/v1/admin/partner-settlements", GetPartnerSettlementsAsync)
            .RequireAuthorization("AdminOnly");

        app.MapGet("/api/v1/admin/partner-settlements/{settlementId:guid}", GetPartnerSettlementByIdAsync)
            .RequireAuthorization("AdminOnly");

        app.MapGet("/api/v1/admin/partners/{partnerId:guid}/settlements", GetPartnerSettlementsByPartnerAsync)
            .RequireAuthorization("AdminOnly");

        app.MapPost("/api/v1/admin/partner-settlements/{settlementId:guid}/mark-paid", MarkSettlementPaidAsync)
            .RequireAuthorization("AdminOnly");

        app.MapGet("/api/v1/admin/partner-settlements/summary", GetPartnerSettlementSummaryAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetPartnerSettlementsAsync(
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] Guid? partnerId,
        [FromQuery] bool? overdue,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = db.PartnerSettlements
            .Include(s => s.IntegrationPartner)
            .Include(s => s.Booking)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.ToLowerInvariant());

        if (partnerId.HasValue)
            query = query.Where(s => s.IntegrationPartnerId == partnerId.Value);

        if (overdue.HasValue && overdue.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(s => s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < now);
        }

        var total = await query.CountAsync();
        var settlements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new
        {
            total,
            page,
            pageSize,
            data = settlements.Select(s => new
            {
                s.Id,
                s.IntegrationPartnerId,
                partnerName = s.IntegrationPartner?.Name,
                s.BookingId,
                s.BookingReference,
                s.SettlementPeriodStart,
                s.SettlementPeriodEnd,
                s.TotalAmount,
                s.CommissionPercent,
                s.CommissionAmount,
                s.SettlementAmount,
                s.Status,
                s.DueDate,
                s.PaidDate,
                s.PaymentReference,
                s.PaymentMethod,
                s.Notes,
                s.CreatedAt,
                isOverdue = s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow
            })
        });
    }

    private static async Task<IResult> GetPartnerSettlementByIdAsync(
        Guid settlementId,
        AppDbContext db)
    {
        var settlement = await db.PartnerSettlements
            .Include(s => s.IntegrationPartner)
            .Include(s => s.Booking)
                .ThenInclude(b => b.Vehicle)
            .Include(s => s.Booking)
                .ThenInclude(b => b.Renter)
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (settlement is null)
            return Results.NotFound(new { error = "Settlement not found" });

        return Results.Ok(new
        {
            settlement.Id,
            partner = new
            {
                settlement.IntegrationPartnerId,
                name = settlement.IntegrationPartner?.Name,
                type = settlement.IntegrationPartner?.Type
            },
            booking = new
            {
                settlement.BookingId,
                settlement.BookingReference,
                vehicleName = settlement.Booking?.Vehicle != null 
                    ? $"{settlement.Booking.Vehicle.Make} {settlement.Booking.Vehicle.Model}"
                    : null,
                renterName = settlement.Booking?.Renter != null
                    ? $"{settlement.Booking.Renter.FirstName} {settlement.Booking.Renter.LastName}"
                    : null,
                pickupDate = settlement.Booking?.PickupDateTime,
                returnDate = settlement.Booking?.ReturnDateTime
            },
            financial = new
            {
                settlement.TotalAmount,
                settlement.CommissionPercent,
                settlement.CommissionAmount,
                settlement.SettlementAmount,
                settlement.Currency
            },
            settlement.Status,
            settlement.DueDate,
            settlement.PaidDate,
            settlement.PaymentReference,
            settlement.PaymentMethod,
            settlement.Notes,
            settlement.CreatedAt,
            settlement.UpdatedAt,
            isOverdue = settlement.Status == "pending" && settlement.DueDate.HasValue && settlement.DueDate.Value < DateTime.UtcNow
        });
    }

    private static async Task<IResult> GetPartnerSettlementsByPartnerAsync(
        Guid partnerId,
        AppDbContext db,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var partner = await db.IntegrationPartners.FindAsync(partnerId);
        if (partner is null)
            return Results.NotFound(new { error = "Partner not found" });

        var query = db.PartnerSettlements
            .Include(s => s.Booking)
            .Where(s => s.IntegrationPartnerId == partnerId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.ToLowerInvariant());

        var total = await query.CountAsync();
        var settlements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPending = await query.Where(s => s.Status == "pending").SumAsync(s => s.SettlementAmount);
        var totalPaid = await query.Where(s => s.Status == "paid").SumAsync(s => s.SettlementAmount);

        return Results.Ok(new
        {
            partner = new
            {
                partnerId,
                partner.Name,
                partner.Type,
                partner.CommissionPercent
            },
            summary = new
            {
                totalSettlements = total,
                totalPending,
                totalPaid,
                overdueCount = await query.CountAsync(s => s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow)
            },
            total,
            page,
            pageSize,
            data = settlements.Select(s => new
            {
                s.Id,
                s.BookingReference,
                s.TotalAmount,
                s.CommissionAmount,
                s.SettlementAmount,
                s.Status,
                s.DueDate,
                s.PaidDate,
                s.CreatedAt,
                isOverdue = s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow
            })
        });
    }

    private static async Task<IResult> MarkSettlementPaidAsync(
        Guid settlementId,
        [FromBody] MarkSettlementPaidRequest request,
        AppDbContext db)
    {
        var settlement = await db.PartnerSettlements
            .Include(s => s.Booking)
            .FirstOrDefaultAsync(s => s.Id == settlementId);

        if (settlement is null)
            return Results.NotFound(new { error = "Settlement not found" });

        if (settlement.Status == "paid")
            return Results.BadRequest(new { error = "Settlement already marked as paid" });

        settlement.Status = "paid";
        settlement.PaidDate = DateTime.UtcNow;
        settlement.PaymentReference = request.PaymentReference;
        settlement.PaymentMethod = request.PaymentMethod;
        settlement.Notes = request.Notes;
        settlement.UpdatedAt = DateTime.UtcNow;

        // Update booking settlement status
        if (settlement.Booking != null)
        {
            settlement.Booking.PartnerSettlementStatus = "paid";
        }

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Settlement marked as paid",
            settlement.Id,
            settlement.SettlementAmount,
            settlement.PaidDate,
            settlement.PaymentReference
        });
    }

    private static async Task<IResult> GetPartnerSettlementSummaryAsync(
        AppDbContext db,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var settlements = await db.PartnerSettlements
            .Include(s => s.IntegrationPartner)
            .Where(s => s.CreatedAt >= fromDate && s.CreatedAt <= toDate)
            .ToListAsync();

        var totalAmount = settlements.Sum(s => s.TotalAmount);
        var totalCommission = settlements.Sum(s => s.CommissionAmount);
        var totalSettlement = settlements.Sum(s => s.SettlementAmount);
        var pendingAmount = settlements.Where(s => s.Status == "pending").Sum(s => s.SettlementAmount);
        var paidAmount = settlements.Where(s => s.Status == "paid").Sum(s => s.SettlementAmount);
        var overdueAmount = settlements.Where(s => s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow).Sum(s => s.SettlementAmount);

        var byPartner = settlements
            .GroupBy(s => new { s.IntegrationPartnerId, PartnerName = s.IntegrationPartner!.Name })
            .Select(g => new
            {
                partnerId = g.Key.IntegrationPartnerId,
                partnerName = g.Key.PartnerName,
                totalBookings = g.Count(),
                totalAmount = g.Sum(s => s.TotalAmount),
                commissionAmount = g.Sum(s => s.CommissionAmount),
                settlementAmount = g.Sum(s => s.SettlementAmount),
                pendingAmount = g.Where(s => s.Status == "pending").Sum(s => s.SettlementAmount),
                paidAmount = g.Where(s => s.Status == "paid").Sum(s => s.SettlementAmount)
            })
            .OrderByDescending(x => x.settlementAmount)
            .ToList();

        return Results.Ok(new
        {
            period = new { from = fromDate, to = toDate },
            summary = new
            {
                totalBookings = settlements.Count,
                totalAmount,
                totalCommission,
                totalSettlement,
                pendingAmount,
                paidAmount,
                overdueAmount,
                overdueCount = settlements.Count(s => s.Status == "pending" && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow)
            },
            byPartner
        });
    }
}

public record MarkSettlementPaidRequest(
    string PaymentReference,
    string? PaymentMethod,
    string? Notes
);
