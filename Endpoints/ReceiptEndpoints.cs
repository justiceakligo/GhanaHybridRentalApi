using System.Security.Claims;
using System.Text;
using GhanaHybridRentalApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class ReceiptEndpoints
{
    public static void MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bookings/{bookingId:guid}/receipt")
            .RequireAuthorization()
            .WithTags("Receipts");

        group.MapGet("", GetReceiptAsync)
            .WithName("GetReceipt")
            .WithDescription("Get booking receipt as JSON");

        group.MapGet("/text", GetReceiptTextAsync)
            .WithName("GetReceiptText")
            .WithDescription("Get booking receipt as nicely formatted text");

        group.MapGet("/pdf", GetReceiptPdfAsync)
            .WithName("GetReceiptPdf")
            .WithDescription("Download booking receipt as PDF");

        group.MapPost("/email", EmailReceiptAsync)
            .WithName("EmailReceipt")
            .WithDescription("Email receipt to customer");
    }

    private static async Task<IResult> GetReceiptAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .Include(b => b.PickupInspection)
            .Include(b => b.ReturnInspection)
            .Include(b => b.InsurancePlan)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check access rights
        var role = user.Role ?? string.Empty;
        var hasAccess = role == "admin" ||
                       booking.RenterId == userId ||
                       booking.OwnerId == userId ||
                       booking.DriverId == userId;

        if (!hasAccess)
            return Results.Forbid();

        // Build receipt data
        var receipt = new
        {
            receiptNumber = $"RCT-{booking.CreatedAt.Year}-{booking.Id.ToString().Substring(0, 8).ToUpper()}",
            receiptDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            bookingReference = booking.BookingReference,

            // Company info
            companyName = "Ryve Rental",
            companyAddress = "Accra, Ghana",
            companyPhone = "+233 XX XXX XXXX",
            companyEmail = "support@ryverental.com",

            // Customer info
            customer = new
            {
                name = $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim(),
                phone = booking.Renter?.Phone,
                email = booking.Renter?.Email
            },

            // Vehicle info
            vehicle = new
            {
                make = booking.Vehicle?.Make,
                model = booking.Vehicle?.Model,
                year = booking.Vehicle?.Year,
                plateNumber = booking.Vehicle?.PlateNumber,
                category = booking.Vehicle?.Category?.Name
            },

            // Driver info (if applicable)
            driver = booking.WithDriver && booking.Driver != null ? new
            {
                name = $"{booking.Driver.FirstName} {booking.Driver.LastName}".Trim(),
                phone = booking.Driver.Phone,
                photoUrl = booking.Driver.DriverProfile?.PhotoUrl,
                rating = booking.Driver.DriverProfile?.AverageRating,
                yearsExperience = booking.Driver.DriverProfile?.YearsOfExperience,
                bio = booking.Driver.DriverProfile?.Bio
            } : null,

            // Trip details
            trip = new
            {
                pickupDate = booking.PickupDateTime.ToString("yyyy-MM-dd"),
                pickupTime = booking.PickupDateTime.ToString("HH:mm"),
                returnDate = booking.ReturnDateTime.ToString("yyyy-MM-dd"),
                returnTime = booking.ReturnDateTime.ToString("HH:mm"),
                totalDays = (booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days
            },

            // Pricing breakdown
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

            // Payment info
            payment = new
            {
                paymentMethod = booking.PaymentMethod,
                paymentStatus = booking.PaymentStatus,
                paymentDate = booking.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            },

            // Inspection details
            preRentalInspection = booking.PickupInspection != null ? new
            {
                date = booking.PickupInspection.CompletedAt?.ToString("yyyy-MM-dd HH:mm"),
                fuelLevel = booking.PickupInspection.FuelLevel,
                mileage = booking.PickupInspection.Mileage,
                damageNotes = booking.PickupInspection.DamageNotesJson,
                photos = booking.PickupInspection.PhotosJson != null 
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(booking.PickupInspection.PhotosJson) 
                    : Array.Empty<string>()
            } : null,

            postRentalInspection = booking.ReturnInspection != null ? new
            {
                date = booking.ReturnInspection.CompletedAt?.ToString("yyyy-MM-dd HH:mm"),
                fuelLevel = booking.ReturnInspection.FuelLevel,
                mileage = booking.ReturnInspection.Mileage,
                damageNotes = booking.ReturnInspection.DamageNotesJson,
                photos = booking.ReturnInspection.PhotosJson != null 
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(booking.ReturnInspection.PhotosJson) 
                    : Array.Empty<string>()
            } : null,

            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        return Results.Ok(receipt);
    }

    private static async Task<IResult> GetReceiptTextAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        Services.IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .Include(b => b.PickupInspection)
            .Include(b => b.ReturnInspection)
            .Include(b => b.InsurancePlan)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check access rights
        var role = user.Role ?? string.Empty;
        var hasAccess = role == "admin" ||
                       booking.RenterId == userId ||
                       booking.OwnerId == userId ||
                       booking.DriverId == userId;

        if (!hasAccess)
            return Results.Forbid();
        var receiptText = await GenerateReceiptTextAsync(booking, configService);
        return Results.Content(receiptText, "text/plain");
    }

    private static async Task<IResult> GetReceiptPdfAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        AppDbContext db,
        Services.IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check access rights
        var hasAccess = user.Role == "admin" ||
                       booking.RenterId == userId ||
                       booking.OwnerId == userId ||
                       booking.DriverId == userId;

        if (!hasAccess)
            return Results.Forbid();

        // Note: PDF generation requires a library like QuestPDF
        // Currently generating text-based receipt
        var role = user.Role ?? string.Empty;
        var receiptText = await GenerateReceiptTextAsync(booking, configService);
        var bytes = Encoding.UTF8.GetBytes(receiptText);

        return Results.File(
            bytes,
            "text/plain",
            $"Receipt-{booking.BookingReference}.txt"
        );
    }

    private static async Task<IResult> EmailReceiptAsync(
        Guid bookingId,
        ClaimsPrincipal principal,
        [FromBody] EmailReceiptRequest request,
        AppDbContext db,
        Services.IEmailService emailService,
        Services.IAppConfigService configService)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.Unauthorized();

        var booking = await db.Bookings
            .Include(b => b.Vehicle)
                .ThenInclude(v => v!.Category)
            .Include(b => b.Renter)
            .Include(b => b.Driver)
                .ThenInclude(d => d!.DriverProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking is null)
            return Results.NotFound(new { error = "Booking not found" });

        // Check access rights
        var role = user.Role ?? string.Empty;
        var hasAccess = role == "admin" ||
                       booking.RenterId == userId ||
                       booking.OwnerId == userId ||
                       booking.DriverId == userId;

        if (!hasAccess)
            return Results.Forbid();

        // Determine recipient email
        var recipientEmail = request.Email ?? booking.Renter?.Email;
        if (string.IsNullOrWhiteSpace(recipientEmail))
            return Results.BadRequest(new { error = "No email address provided" });

        try
        {
            // Generate receipt text
            var receiptText = await GenerateReceiptTextAsync(booking, configService);
            
            // Send email using real email service
            await emailService.SendBookingConfirmationAsync(recipientEmail, receiptText);

            return Results.Ok(new { message = $"Receipt sent to {recipientEmail}" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to send receipt email: {ex.Message}");
        }
    }

    private static async Task<string> GenerateReceiptTextAsync(Models.Booking booking, Services.IAppConfigService configService)
    {
        var currency = await configService.GetConfigValueAsync("Payment:Currency") ?? booking.Currency ?? "GHS";
        
        var sb = new StringBuilder();
        var receiptNumber = $"RCT-{booking.CreatedAt.Year}-{booking.Id.ToString().Substring(0, 8).ToUpper()}";
        
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine("               RYVE RENTAL");
        sb.AppendLine("          Vehicle Rental Receipt");
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Receipt #: {receiptNumber}");
        sb.AppendLine($"Booking Ref: {booking.BookingReference}");
        sb.AppendLine($"Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("CUSTOMER INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Name: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
        sb.AppendLine($"Phone: {booking.Renter?.Phone}");
        sb.AppendLine($"Email: {booking.Renter?.Email}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("RENTAL PERIOD");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Pickup: {booking.PickupDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Return: {booking.ReturnDateTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Duration: {(booking.ReturnDateTime - booking.PickupDateTime).Days} day(s)");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("VEHICLE INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}");
        sb.AppendLine($"Plate: {booking.Vehicle?.PlateNumber}");
        sb.AppendLine();

        if (booking.WithDriver && booking.Driver != null)
        {
            sb.AppendLine("───────────────────────────────────────────────");
            sb.AppendLine("DRIVER INFORMATION");
            sb.AppendLine("───────────────────────────────────────────────");
            sb.AppendLine($"Name: {booking.Driver.FirstName} {booking.Driver.LastName}");
            sb.AppendLine($"Phone: {booking.Driver.Phone}");
            if (booking.Driver.DriverProfile?.AverageRating.HasValue == true)
                sb.AppendLine($"Rating: {booking.Driver.DriverProfile.AverageRating:F1}/5.0");
            sb.AppendLine();
        }

        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("PRICING BREAKDOWN");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Vehicle Rental: {currency} {booking.RentalAmount:F2}");
        if (booking.DriverAmount.HasValue && booking.DriverAmount.Value > 0)
            sb.AppendLine($"Driver Service: {currency} {booking.DriverAmount:F2}");
        if (booking.InsuranceAmount.HasValue && booking.InsuranceAmount.Value > 0)
            sb.AppendLine($"Insurance: {currency} {booking.InsuranceAmount:F2}");
        if (booking.PlatformFee.HasValue && booking.PlatformFee.Value > 0)
            sb.AppendLine($"Platform Fee: {currency} {booking.PlatformFee:F2}");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"TOTAL AMOUNT: {currency} {booking.TotalAmount:F2}");
        sb.AppendLine();
        sb.AppendLine($"Payment Status: {booking.PaymentStatus.ToUpper()}");
        sb.AppendLine($"Payment Method: {booking.PaymentMethod}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine("Thank you for choosing Ryve Rental!");
        sb.AppendLine("═══════════════════════════════════════════════");
        
        return sb.ToString();
    }
}

public record EmailReceiptRequest(
    string? Email,
    bool IncludeDriverDetails = true,
    bool IncludeInspectionPhotos = true
);
