using System.Security.Claims;
using System.Text;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class ReceiptTemplateEndpoints
{
    public static void MapReceiptTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/receipt-templates")
            .RequireAuthorization("AdminOnly")
            .WithTags("Receipt Templates");

        group.MapGet("", GetAllTemplatesAsync)
            .WithName("GetAllReceiptTemplates")
            .WithDescription("Get all receipt templates (admin)");

        group.MapGet("/active", GetActiveTemplateAsync)
            .WithName("GetActiveReceiptTemplate")
            .WithDescription("Get currently active receipt template (admin)");

        group.MapGet("/{id:guid}", GetTemplateByIdAsync)
            .WithName("GetReceiptTemplateById")
            .WithDescription("Get receipt template by ID (admin)");

        group.MapPost("", CreateTemplateAsync)
            .WithName("CreateReceiptTemplate")
            .WithDescription("Create new receipt template (admin)");

        group.MapPut("/{id:guid}", UpdateTemplateAsync)
            .WithName("UpdateReceiptTemplate")
            .WithDescription("Update receipt template (admin)");

        group.MapDelete("/{id:guid}", DeleteTemplateAsync)
            .WithName("DeleteReceiptTemplate")
            .WithDescription("Delete receipt template (admin)");

        group.MapPost("/{id:guid}/activate", ActivateTemplateAsync)
            .WithName("ActivateReceiptTemplate")
            .WithDescription("Activate receipt template (deactivates others) (admin)");

        group.MapGet("/preview/{bookingId:guid}", PreviewReceiptAsync)
            .WithName("PreviewReceipt")
            .WithDescription("Preview receipt with current active template (admin)");
        // Admin receipt download for any booking
        var adminReceiptGroup = app.MapGroup("/api/v1/admin/receipts")
            .RequireAuthorization("AdminOnly")
            .WithTags("Receipts - Admin");

        adminReceiptGroup.MapGet("/bookings/{bookingId:guid}/pdf", DownloadReceiptForBookingAsync)
            .WithName("AdminDownloadReceipt")
            .WithDescription("Admin: Download receipt for any booking as PDF");

        adminReceiptGroup.MapGet("/bookings/{bookingId:guid}/json", GetReceiptDataForBookingAsync)
            .WithName("AdminGetReceiptData")
            .WithDescription("Admin: Get receipt data for any booking as JSON");    }

    private static async Task<IResult> GetAllTemplatesAsync(
        IReceiptTemplateService templateService)
    {
        var templates = await templateService.GetAllTemplatesAsync();
        return Results.Ok(templates);
    }

    private static async Task<IResult> GetActiveTemplateAsync(
        IReceiptTemplateService templateService)
    {
        var template = await templateService.GetActiveTemplateAsync();
        if (template == null)
            return Results.NotFound(new { error = "No active template found" });

        return Results.Ok(template);
    }

    private static async Task<IResult> GetTemplateByIdAsync(
        Guid id,
        AppDbContext db)
    {
        var template = await db.ReceiptTemplates.FindAsync(id);
        if (template == null)
            return Results.NotFound(new { error = "Template not found" });

        return Results.Ok(template);
    }

    private static async Task<IResult> CreateTemplateAsync(
        [FromBody] CreateReceiptTemplateRequest request,
        ClaimsPrincipal principal,
        IReceiptTemplateService templateService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var template = new ReceiptTemplate
        {
            TemplateName = request.TemplateName,
            LogoUrl = request.LogoUrl ?? "https://i.imgur.com/ryvepool-logo.png",
            CompanyName = request.CompanyName,
            CompanyAddress = request.CompanyAddress ?? "Accra, Ghana",
            CompanyPhone = request.CompanyPhone ?? "+233 XX XXX XXXX",
            CompanyEmail = request.CompanyEmail ?? "support@ryvepool.com",
            CompanyWebsite = request.CompanyWebsite ?? "www.ryvepool.com",
            HeaderTemplate = request.HeaderTemplate ?? "",
            FooterTemplate = request.FooterTemplate ?? "",
            TermsAndConditions = request.TermsAndConditions,
            CustomCss = request.CustomCss,
            IsActive = request.IsActive,
            ShowLogo = request.ShowLogo,
            ShowQrCode = request.ShowQrCode,
            ReceiptNumberPrefix = request.ReceiptNumberPrefix ?? "RCT",
            CreatedByUserId = userId
        };

        var created = await templateService.CreateOrUpdateTemplateAsync(template);
        return Results.Created($"/api/v1/admin/receipt-templates/{created.Id}", created);
    }

    private static async Task<IResult> UpdateTemplateAsync(
        Guid id,
        [FromBody] UpdateReceiptTemplateRequest request,
        AppDbContext db,
        IReceiptTemplateService templateService)
    {
        var existing = await db.ReceiptTemplates.FindAsync(id);
        if (existing == null)
            return Results.NotFound(new { error = "Template not found" });

        // Update fields
        if (request.TemplateName != null)
            existing.TemplateName = request.TemplateName;
        if (request.LogoUrl != null)
            existing.LogoUrl = request.LogoUrl;
        if (request.CompanyName != null)
            existing.CompanyName = request.CompanyName;
        if (request.CompanyAddress != null)
            existing.CompanyAddress = request.CompanyAddress;
        if (request.CompanyPhone != null)
            existing.CompanyPhone = request.CompanyPhone;
        if (request.CompanyEmail != null)
            existing.CompanyEmail = request.CompanyEmail;
        if (request.CompanyWebsite != null)
            existing.CompanyWebsite = request.CompanyWebsite;
        if (request.HeaderTemplate != null)
            existing.HeaderTemplate = request.HeaderTemplate;
        if (request.FooterTemplate != null)
            existing.FooterTemplate = request.FooterTemplate;
        if (request.TermsAndConditions != null)
            existing.TermsAndConditions = request.TermsAndConditions;
        if (request.CustomCss != null)
            existing.CustomCss = request.CustomCss;
        if (request.IsActive.HasValue)
            existing.IsActive = request.IsActive.Value;
        if (request.ShowLogo.HasValue)
            existing.ShowLogo = request.ShowLogo.Value;
        if (request.ShowQrCode.HasValue)
            existing.ShowQrCode = request.ShowQrCode.Value;
        if (request.ReceiptNumberPrefix != null)
            existing.ReceiptNumberPrefix = request.ReceiptNumberPrefix;

        var updated = await templateService.CreateOrUpdateTemplateAsync(existing);
        return Results.Ok(updated);
    }

    private static async Task<IResult> DeleteTemplateAsync(
        Guid id,
        IReceiptTemplateService templateService)
    {
        var deleted = await templateService.DeleteTemplateAsync(id);
        if (!deleted)
            return Results.NotFound(new { error = "Template not found" });

        return Results.Ok(new { message = "Template deleted successfully" });
    }

    private static async Task<IResult> ActivateTemplateAsync(
        Guid id,
        AppDbContext db,
        IReceiptTemplateService templateService)
    {
        var template = await db.ReceiptTemplates.FindAsync(id);
        if (template == null)
            return Results.NotFound(new { error = "Template not found" });

        // Deactivate all templates
        var allTemplates = await db.ReceiptTemplates.ToListAsync();
        foreach (var t in allTemplates)
        {
            t.IsActive = false;
        }

        // Activate this one
        template.IsActive = true;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Template activated successfully", template });
    }

    private static async Task<IResult> PreviewReceiptAsync(
        Guid bookingId,
        AppDbContext db,
        IReceiptTemplateService templateService)
    {
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        var html = await templateService.GenerateReceiptHtmlAsync(booking);
        return Results.Content(html, "text/html");
    }

    // Admin endpoints for receipt access
    private static async Task<IResult> DownloadReceiptForBookingAsync(
        Guid bookingId,
        AppDbContext db,
        IReceiptTemplateService templateService)
    {
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        // Generate real PDF using your template service
        var pdfBytes = await templateService.GenerateReceiptPdfAsync(booking);

        return Results.File(
            pdfBytes,
            "application/pdf",
            $"receipt-{booking.BookingReference}.pdf"
        );
    }

    private static async Task<IResult> GetReceiptDataForBookingAsync(
        Guid bookingId,
        AppDbContext db)
    {
        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            return Results.NotFound(new { error = "Booking not found" });

        var receipt = new
        {
            receiptNumber = $"RCT-{booking.CreatedAt.Year}-{booking.Id.ToString().Substring(0, 8).ToUpper()}",
            receiptDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            bookingReference = booking.BookingReference,
            customer = new
            {
                name = $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim(),
                phone = booking.Renter?.Phone,
                email = booking.Renter?.Email
            },
            vehicle = new
            {
                make = booking.Vehicle?.Make,
                model = booking.Vehicle?.Model,
                year = booking.Vehicle?.Year,
                plateNumber = booking.Vehicle?.PlateNumber,
                category = booking.Vehicle?.Category?.Name
            },
            trip = new
            {
                pickupDate = booking.PickupDateTime.ToString("yyyy-MM-dd"),
                pickupTime = booking.PickupDateTime.ToString("HH:mm"),
                returnDate = booking.ReturnDateTime.ToString("yyyy-MM-dd"),
                returnTime = booking.ReturnDateTime.ToString("HH:mm"),
                totalDays = (booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days
            },
            pricing = new
            {
                vehicleSubtotal = booking.RentalAmount,
                driverSubtotal = booking.DriverAmount ?? 0m,
                insuranceSubtotal = booking.InsuranceAmount ?? 0m,
                depositAmount = booking.DepositAmount,
                platformFee = booking.PlatformFee ?? 0m,
                totalAmount = booking.TotalAmount,
                currency = booking.Currency
            },
            payment = new
            {
                paymentMethod = booking.PaymentMethod,
                paymentStatus = booking.PaymentStatus,
                paymentDate = booking.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }
        };

        return Results.Ok(receipt);
    }
}

public record CreateReceiptTemplateRequest(
    string TemplateName,
    string? LogoUrl,
    string CompanyName,
    string? CompanyAddress,
    string? CompanyPhone,
    string? CompanyEmail,
    string? CompanyWebsite,
    string? HeaderTemplate,
    string? FooterTemplate,
    string? TermsAndConditions,
    string? CustomCss,
    bool IsActive,
    bool ShowLogo,
    bool ShowQrCode,
    string? ReceiptNumberPrefix
);

public record UpdateReceiptTemplateRequest(
    string? TemplateName,
    string? LogoUrl,
    string? CompanyName,
    string? CompanyAddress,
    string? CompanyPhone,
    string? CompanyEmail,
    string? CompanyWebsite,
    string? HeaderTemplate,
    string? FooterTemplate,
    string? TermsAndConditions,
    string? CustomCss,
    bool? IsActive,
    bool? ShowLogo,
    bool? ShowQrCode,
    string? ReceiptNumberPrefix
);
