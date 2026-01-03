using System.Security.Claims;
using System.Text.Json;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class RentalAgreementEndpoints
{
    public static void MapRentalAgreementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");

        // Admin template management
        group.MapGet("/admin/rental-agreement-template", GetTemplateAsync)
            .RequireAuthorization("AdminOnly");

        group.MapPut("/admin/rental-agreement-template", UpdateTemplateAsync)
            .RequireAuthorization("AdminOnly");

        // Allow anonymous booking agreement viewing and acceptance for guests
        group.MapGet("/bookings/{bookingId:guid}/rental-agreement", GetBookingAgreementAsync);
        group.MapPost("/bookings/{bookingId:guid}/rental-agreement/accept", AcceptAgreementAsync);
        group.MapGet("/bookings/{bookingId:guid}/rental-agreement/acceptance", GetAcceptanceAsync)
            .RequireAuthorization(); // owner/admin/renter with access
    }

    private static async Task<IResult> GetTemplateAsync(AppDbContext db)
    {
        var template = await db.RentalAgreementTemplates
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync();

        if (template is null)
        {
            template = new RentalAgreementTemplate
            {
                Code = "default",
                Version = "1.0.0",
                Title = "Ryve Rental Agreement",
                BodyText =
                    "RYVE RENTAL AGREEMENT\n\n" +
                    "By renting a vehicle with Ryve Rental, you agree to the following terms and conditions:\n\n" +
                    "1. NO SMOKING POLICY\n" +
                    "Smoking (including vaping and e-cigarettes) is strictly prohibited in all rental vehicles. " +
                    "If evidence of smoking is detected, a cleaning fee of GHS 500 will be charged to your account.\n\n" +
                    "2. FINES, TICKETS, AND PENALTIES\n" +
                    "You are fully responsible for all traffic fines, parking tickets, tolls, clamping fees, " +
                    "towing charges, and any other penalties incurred during your rental period. " +
                    "These charges will be deducted from your security deposit or charged to your payment method.\n\n" +
                    "3. ACCIDENT AND INCIDENT PROCEDURE\n" +
                    "In the event of an accident or incident:\n" +
                    "- Stop immediately and ensure safety of all parties\n" +
                    "- Contact local police if there are injuries or significant damage\n" +
                    "- Contact Ryve Rental within 24 hours: +233 XX XXX XXXX\n" +
                    "- Do not admit fault or liability\n" +
                    "- Take photos and exchange information with other parties\n" +
                    "- Complete an accident report form\n\n" +
                    "4. FUEL POLICY\n" +
                    "The vehicle will be provided with a specified fuel level. You must return the vehicle with " +
                    "the same fuel level. Failure to do so will result in refueling charges plus a service fee.\n\n" +
                    "5. VEHICLE CONDITION\n" +
                    "You are responsible for the vehicle's condition during the rental period. Any damage beyond " +
                    "normal wear and tear will be charged to you based on repair costs.\n\n" +
                    "6. TIMELY RETURN\n" +
                    "The vehicle must be returned at the agreed time and location. Late returns will incur " +
                    "additional charges at the daily rate plus a late fee.\n\n" +
                    "By accepting this agreement, you acknowledge that you have read, understood, and agree to " +
                    "comply with all terms and conditions."
            };

            db.RentalAgreementTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        return Results.Ok(new RentalAgreementTemplateDto(
            template.Id,
            template.Code,
            template.Version,
            template.Title,
            template.BodyText,
            template.RequireNoSmokingConfirmation,
            template.RequireFinesAndTicketsConfirmation,
            template.RequireAccidentProcedureConfirmation
        ));
    }

    private static async Task<IResult> UpdateTemplateAsync(
        [FromBody] RentalAgreementTemplateDto dto,
        AppDbContext db)
    {
        var template = new RentalAgreementTemplate
        {
            Code = dto.Code,
            Version = dto.Version,
            Title = dto.Title,
            BodyText = dto.BodyText,
            RequireNoSmokingConfirmation = dto.RequireNoSmokingConfirmation,
            RequireFinesAndTicketsConfirmation = dto.RequireFinesAndTicketsConfirmation,
            RequireAccidentProcedureConfirmation = dto.RequireAccidentProcedureConfirmation,
            UpdatedAt = DateTime.UtcNow
        };

        db.RentalAgreementTemplates.Add(template);
        await db.SaveChangesAsync();

        return Results.Ok(new RentalAgreementTemplateDto(
            template.Id,
            template.Code,
            template.Version,
            template.Title,
            template.BodyText,
            template.RequireNoSmokingConfirmation,
            template.RequireFinesAndTicketsConfirmation,
            template.RequireAccidentProcedureConfirmation
        ));
    }

    private static async Task<IResult> GetBookingAgreementAsync(
        Guid bookingId,
        AppDbContext db)
    {
        var booking = await db.Bookings
            .Include(b => b.Renter)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        var template = await db.RentalAgreementTemplates
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync();

        if (template is null)
            return Results.NotFound(new { error = "Rental agreement template not configured" });

        var accepted = await db.RentalAgreementAcceptances
            .AnyAsync(a => a.BookingId == bookingId);

        var view = new BookingRentalAgreementView(
            booking.Id,
            booking.BookingReference,
            template.Code,
            template.Version,
            template.Title,
            template.BodyText,
            template.RequireNoSmokingConfirmation,
            template.RequireFinesAndTicketsConfirmation,
            template.RequireAccidentProcedureConfirmation,
            accepted
        );

        return Results.Ok(view);
    }

    private static async Task<IResult> AcceptAgreementAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        HttpContext httpContext,
        [FromBody] JsonElement body,
        AppDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("RentalAgreementEndpoints");
        // Log minimal request info to help debug; do not log sensitive headers
        try
        {
            logger.LogInformation("AcceptAgreement called for booking {BookingId}. Authorization present={Auth}", bookingId, httpContext.Request.Headers.ContainsKey("Authorization"));
            logger.LogInformation("AcceptAgreement body: {Body}", body.GetRawText());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to log accept agreement request");
        }

        AcceptRentalAgreementRequest request;
        try
        {
            request = JsonSerializer.Deserialize<AcceptRentalAgreementRequest>(body.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new Exception("Deserialized request was null");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize AcceptRentalAgreementRequest");
            return Results.BadRequest(new { error = "Invalid request payload for rental agreement acceptance" });
        }

        // Validate required clause checkboxes
        var template = await db.RentalAgreementTemplates.OrderByDescending(t => t.UpdatedAt).FirstOrDefaultAsync();
        if (template is null)
            return Results.BadRequest(new { error = "Rental agreement template not configured" });

        if (template.RequireNoSmokingConfirmation && !request.AcceptedNoSmoking)
            return Results.BadRequest(new { error = "You must accept the no-smoking clause" });
        if (template.RequireFinesAndTicketsConfirmation && !request.AcceptedFinesAndTickets)
            return Results.BadRequest(new { error = "You must accept responsibility for fines and tickets" });
        if (template.RequireAccidentProcedureConfirmation && !request.AcceptedAccidentProcedure)
            return Results.BadRequest(new { error = "You must accept the accident procedure clause" });

        // Authenticated renter: must match booking renter
        var userIdStr = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var authRenterId))
        {
            var booking = await db.Bookings.Include(b => b.Renter).FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking is null) return Results.NotFound(new { error = "Booking not found" });
            if (booking.RenterId != authRenterId) return Results.Forbid();

            var already = await db.RentalAgreementAcceptances.FirstOrDefaultAsync(a => a.BookingId == bookingId && a.RenterId == authRenterId);
            if (already is not null) return Results.Ok(new RentalAgreementAcceptanceDto(already.BookingId, already.RenterId, already.TemplateCode, already.TemplateVersion, already.AcceptedNoSmoking, already.AcceptedFinesAndTickets, already.AcceptedAccidentProcedure, already.AcceptedAt, already.IpAddress));

            var acceptance = new RentalAgreementAcceptance
            {
                BookingId = booking.Id,
                RenterId = authRenterId,
                TemplateCode = template.Code,
                TemplateVersion = template.Version,
                AcceptedNoSmoking = request.AcceptedNoSmoking,
                AcceptedFinesAndTickets = request.AcceptedFinesAndTickets,
                AcceptedAccidentProcedure = request.AcceptedAccidentProcedure,
                AgreementSnapshot = template.BodyText,
                AcceptedAt = DateTime.UtcNow,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString()
            };

            db.RentalAgreementAcceptances.Add(acceptance);
            await db.SaveChangesAsync();

            return Results.Ok(new RentalAgreementAcceptanceDto(acceptance.BookingId, acceptance.RenterId, acceptance.TemplateCode, acceptance.TemplateVersion, acceptance.AcceptedNoSmoking, acceptance.AcceptedFinesAndTickets, acceptance.AcceptedAccidentProcedure, acceptance.AcceptedAt, acceptance.IpAddress));
        }

        // Guest flow: require customerEmail and match booking guest email
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return Results.BadRequest(new { error = "Customer email is required for guest acceptance" });

        var booking2 = await db.Bookings.Include(b => b.Renter).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking2 is null) return Results.NotFound(new { error = "Booking not found" });

        var providedEmail = request.CustomerEmail.Trim().ToLowerInvariant();
        var bookingEmail = booking2.GuestEmail?.Trim().ToLowerInvariant() ?? booking2.Renter?.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(bookingEmail) || bookingEmail != providedEmail)
            return Results.Json(new { error = "Customer email does not match booking" }, statusCode: 401);

        // Ensure a renter User exists and persist acceptance
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == request.CustomerEmail);
        if (existing == null)
        {
            existing = new User { Email = request.CustomerEmail, FirstName = request.CustomerName, Role = "renter", Status = "pending" };
            db.Users.Add(existing);
            await db.SaveChangesAsync();
        }

        booking2.RenterId = existing.Id;
        await db.SaveChangesAsync();

        var alreadyGuest = await db.RentalAgreementAcceptances.FirstOrDefaultAsync(a => a.BookingId == bookingId && a.RenterId == existing.Id);
        if (alreadyGuest is not null) return Results.Ok(new RentalAgreementAcceptanceDto(alreadyGuest.BookingId, alreadyGuest.RenterId, alreadyGuest.TemplateCode, alreadyGuest.TemplateVersion, alreadyGuest.AcceptedNoSmoking, alreadyGuest.AcceptedFinesAndTickets, alreadyGuest.AcceptedAccidentProcedure, alreadyGuest.AcceptedAt, alreadyGuest.IpAddress));

        var guestAcceptance = new RentalAgreementAcceptance
        {
            BookingId = booking2.Id,
            RenterId = existing.Id,
            TemplateCode = template.Code,
            TemplateVersion = template.Version,
            AcceptedNoSmoking = request.AcceptedNoSmoking,
            AcceptedFinesAndTickets = request.AcceptedFinesAndTickets,
            AcceptedAccidentProcedure = request.AcceptedAccidentProcedure,
            AgreementSnapshot = template.BodyText,
            AcceptedAt = DateTime.UtcNow,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString()
        };

        db.RentalAgreementAcceptances.Add(guestAcceptance);
        await db.SaveChangesAsync();

        return Results.Ok(new RentalAgreementAcceptanceDto(guestAcceptance.BookingId, guestAcceptance.RenterId, guestAcceptance.TemplateCode, guestAcceptance.TemplateVersion, guestAcceptance.AcceptedNoSmoking, guestAcceptance.AcceptedFinesAndTickets, guestAcceptance.AcceptedAccidentProcedure, guestAcceptance.AcceptedAt, guestAcceptance.IpAddress));
    }

    private static async Task<IResult> GetAcceptanceAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return Results.NotFound(new { error = "Booking not found" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Results.Unauthorized();

        var allowed = string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || booking.OwnerId == userId || booking.RenterId == userId;
        if (!allowed) return Results.Forbid();

        var acceptance = await db.RentalAgreementAcceptances.FirstOrDefaultAsync(a => a.BookingId == bookingId);
        if (acceptance is null) return Results.NotFound(new { error = "No acceptance recorded for this booking" });

        return Results.Ok(new RentalAgreementAcceptanceDto(acceptance.BookingId, acceptance.RenterId, acceptance.TemplateCode, acceptance.TemplateVersion, acceptance.AcceptedNoSmoking, acceptance.AcceptedFinesAndTickets, acceptance.AcceptedAccidentProcedure, acceptance.AcceptedAt, acceptance.IpAddress));
    }
}

