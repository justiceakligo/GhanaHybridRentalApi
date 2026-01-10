using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GhanaHybridRentalApi.Services;

public interface IReceiptTemplateService
{
    Task<ReceiptTemplate?> GetActiveTemplateAsync();
    Task<List<ReceiptTemplate>> GetAllTemplatesAsync();
    Task<ReceiptTemplate> CreateOrUpdateTemplateAsync(ReceiptTemplate template);
    Task<bool> DeleteTemplateAsync(Guid templateId);
    Task<string> GenerateReceiptHtmlAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null);
    Task<string> GenerateReceiptTextAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null);
    Task<byte[]> GenerateReceiptPdfAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null);
    Task<byte[]> GenerateRentalAgreementPdfAsync(Booking booking, RentalAgreementAcceptance acceptance);
}

public class ReceiptTemplateService : IReceiptTemplateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReceiptTemplateService> _logger;

    public ReceiptTemplateService(AppDbContext db, ILogger<ReceiptTemplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReceiptTemplate?> GetActiveTemplateAsync()
    {
        return await _db.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.IsActive)
            ?? GetDefaultTemplate();
    }

    public async Task<List<ReceiptTemplate>> GetAllTemplatesAsync()
    {
        return await _db.ReceiptTemplates
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReceiptTemplate> CreateOrUpdateTemplateAsync(ReceiptTemplate template)
    {
        var existing = await _db.ReceiptTemplates.FindAsync(template.Id);

        if (existing != null)
        {
            // Update existing
            existing.TemplateName = template.TemplateName;
            existing.LogoUrl = template.LogoUrl;
            existing.CompanyName = template.CompanyName;
            existing.CompanyAddress = template.CompanyAddress;
            existing.CompanyPhone = template.CompanyPhone;
            existing.CompanyEmail = template.CompanyEmail;
            existing.CompanyWebsite = template.CompanyWebsite;
            existing.HeaderTemplate = template.HeaderTemplate;
            existing.FooterTemplate = template.FooterTemplate;
            existing.TermsAndConditions = template.TermsAndConditions;
            existing.CustomCss = template.CustomCss;
            existing.IsActive = template.IsActive;
            existing.ShowLogo = template.ShowLogo;
            existing.ShowQrCode = template.ShowQrCode;
            existing.ReceiptNumberPrefix = template.ReceiptNumberPrefix;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }
        else
        {
            // Create new - deactivate all other templates first
            if (template.IsActive)
            {
                var activeTemplates = await _db.ReceiptTemplates.Where(t => t.IsActive).ToListAsync();
                foreach (var t in activeTemplates)
                {
                    t.IsActive = false;
                }
            }

            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _db.ReceiptTemplates.Add(template);
            await _db.SaveChangesAsync();
            return template;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        var template = await _db.ReceiptTemplates.FindAsync(templateId);
        if (template == null)
            return false;

        _db.ReceiptTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateReceiptHtmlAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null)
    {
        var template = await GetActiveTemplateAsync();
        var placeholders = BuildPlaceholders(booking, additionalPlaceholders);

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset='UTF-8'>");
        html.AppendLine("  <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine("  <title>Receipt</title>");
        html.AppendLine("  <style>");
        html.AppendLine(GetDefaultCss());
        if (!string.IsNullOrWhiteSpace(template?.CustomCss))
        {
            html.AppendLine(template.CustomCss);
        }
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <div class='receipt-container'>");

        // Header with logo
        if (template?.ShowLogo == true && !string.IsNullOrWhiteSpace(template.LogoUrl))
        {
            html.AppendLine("    <div class='header'>");
            html.AppendLine($"      <img src='{template.LogoUrl}' alt='Company Logo' class='logo' />");
            html.AppendLine($"      <h1>{template.CompanyName}</h1>");
            html.AppendLine($"      <p class='company-info'>");
            html.AppendLine($"        {template.CompanyAddress}<br>");
            html.AppendLine($"        {template.CompanyPhone} | {template.CompanyEmail}<br>");
            html.AppendLine($"        {template.CompanyWebsite}");
            html.AppendLine($"      </p>");
            html.AppendLine("    </div>");
        }

        html.AppendLine("    <div class='receipt-title'>");
        html.AppendLine("      <h2>RENTAL RECEIPT</h2>");
        html.AppendLine($"      <p class='receipt-number'>Receipt #: {placeholders["{{receiptNumber}}"]}</p>");
        html.AppendLine($"      <p class='date'>Date: {placeholders["{{receiptDate}}"]}</p>");
        html.AppendLine("    </div>");

        // Customer Info
        html.AppendLine("    <div class='section'>");
        html.AppendLine("      <h3>Customer Information</h3>");
        html.AppendLine($"      <p><strong>Name:</strong> {placeholders["{{customerName}}"]}</p>");
        html.AppendLine($"      <p><strong>Email:</strong> {placeholders["{{customerEmail}}"]}</p>");
        html.AppendLine($"      <p><strong>Phone:</strong> {placeholders["{{customerPhone}}"]}</p>");
        html.AppendLine($"      <p><strong>Booking Reference:</strong> {placeholders["{{bookingReference}}"]}</p>");
        html.AppendLine("    </div>");

        // Rental Period
        html.AppendLine("    <div class='section'>");
        html.AppendLine("      <h3>Rental Period</h3>");
        html.AppendLine($"      <p><strong>Pickup:</strong> {placeholders["{{pickupDateTime}}"]}</p>");
        html.AppendLine($"      <p><strong>Return:</strong> {placeholders["{{returnDateTime}}"]}</p>");
        html.AppendLine($"      <p><strong>Duration:</strong> {placeholders["{{totalDays}}"]} day(s)</p>");
        html.AppendLine("    </div>");

        // Vehicle Info
        html.AppendLine("    <div class='section'>");
        html.AppendLine("      <h3>Vehicle Information</h3>");
        html.AppendLine($"      <p><strong>Vehicle:</strong> {placeholders["{{vehicleName}}"]}</p>");
        html.AppendLine($"      <p><strong>Plate Number:</strong> {placeholders["{{plateNumber}}"]}</p>");
        html.AppendLine("    </div>");

        // Pricing
        html.AppendLine("    <div class='section'>");
        html.AppendLine("      <h3>Pricing Breakdown</h3>");
        html.AppendLine("      <table class='pricing-table'>");
        html.AppendLine($"        <tr><td>Vehicle ({placeholders["{{totalDays}}"]} day(s)):</td><td class='amount'>{placeholders["{{currency}}"]} {placeholders["{{vehicleAmount}}"]}</td></tr>");
        
        if (booking.WithDriver && booking.DriverAmount.HasValue && booking.DriverAmount.Value > 0)
            html.AppendLine($"        <tr><td>Driver Service ({placeholders["{{totalDays}}"]} day(s)):</td><td class='amount'>{placeholders["{{currency}}"]} {placeholders["{{driverAmount}}"]}</td></tr>");
        
        if (booking.ProtectionAmount.HasValue && booking.ProtectionAmount.Value > 0)
            html.AppendLine($"        <tr><td>Protection Plan:</td><td class='amount'>{placeholders["{{currency}}"]} {placeholders["{{protectionAmount}}"]}</td></tr>");
        
        if (booking.PlatformFee.HasValue && booking.PlatformFee.Value > 0)
            html.AppendLine($"        <tr><td>Service Fee:</td><td class='amount'>{placeholders["{{currency}}"]} {placeholders["{{platformFee}}"]}</td></tr>");
        
        if (booking.DepositAmount > 0)
            html.AppendLine($"        <tr><td>Security Deposit (Refundable):</td><td class='amount'>{placeholders["{{currency}}"]} {placeholders["{{depositAmount}}"]}</td></tr>");
        
        if (booking.PromoDiscountAmount.HasValue && booking.PromoDiscountAmount.Value > 0)
            html.AppendLine($"        <tr class='discount'><td>Promo Discount:</td><td class='amount'>-{placeholders["{{currency}}"]} {placeholders["{{discountAmount}}"]}</td></tr>");
        
        html.AppendLine("        <tr class='total'><td><strong>TOTAL:</strong></td><td class='amount'><strong>{placeholders[\"{{currency}}\"]} {placeholders[\"{{totalAmount}}\"]}</strong></td></tr>");
        html.AppendLine("      </table>");
        html.AppendLine("    </div>");

        // Payment Status
        html.AppendLine("    <div class='section'>");
        html.AppendLine($"      <p><strong>Payment Status:</strong> <span class='status-{placeholders["{{paymentStatus}}"].ToLower()}'>{placeholders["{{paymentStatus}}"]}</span></p>");
        html.AppendLine($"      <p><strong>Payment Method:</strong> {placeholders["{{paymentMethod}}"]}</p>");
        html.AppendLine("    </div>");

        // Footer
        if (!string.IsNullOrWhiteSpace(template?.TermsAndConditions))
        {
            html.AppendLine("    <div class='footer'>");
            html.AppendLine($"      <p class='terms'>{template.TermsAndConditions}</p>");
            html.AppendLine("    </div>");
        }

        html.AppendLine("    <div class='footer'>");
        html.AppendLine($"      <p>Thank you for choosing {template?.CompanyName ?? "RyvePool"}!</p>");
        html.AppendLine($"      <p class='generated'>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");
        html.AppendLine("    </div>");

        html.AppendLine("  </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateReceiptTextAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null)
    {
        var template = await GetActiveTemplateAsync();
        var placeholders = BuildPlaceholders(booking, additionalPlaceholders);

        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine($"          {template?.CompanyName ?? "RYVEPOOL"}");
        sb.AppendLine("        Vehicle Rental Receipt");
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Receipt #: {placeholders["{{receiptNumber}}"]}");
        sb.AppendLine($"Booking Ref: {placeholders["{{bookingReference}}"]}");
        sb.AppendLine($"Date: {placeholders["{{receiptDate}}"]}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("CUSTOMER INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Name: {placeholders["{{customerName}}"]}");
        sb.AppendLine($"Phone: {placeholders["{{customerPhone}}"]}");
        sb.AppendLine($"Email: {placeholders["{{customerEmail}}"]}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("RENTAL PERIOD");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Pickup: {placeholders["{{pickupDateTime}}"]}");
        sb.AppendLine($"Return: {placeholders["{{returnDateTime}}"]}");
        sb.AppendLine($"Duration: {placeholders["{{totalDays}}"]} day(s)");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine("VEHICLE INFORMATION");
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"Vehicle: {placeholders["{{vehicleName}}"]}");
        sb.AppendLine($"Plate: {placeholders["{{plateNumber}}"]}");
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
        sb.AppendLine($"Vehicle ({placeholders["{{totalDays}}"]} day(s)): {placeholders["{{currency}}"]} {placeholders["{{vehicleAmount}}"]}");
        
        if (booking.DriverAmount.HasValue && booking.DriverAmount.Value > 0)
            sb.AppendLine($"Driver Service ({placeholders["{{totalDays}}"]} day(s)): {placeholders["{{currency}}"]} {placeholders["{{driverAmount}}"]}");
        
        if (booking.ProtectionAmount.HasValue && booking.ProtectionAmount.Value > 0)
            sb.AppendLine($"Protection Plan: {placeholders["{{currency}}"]} {placeholders["{{protectionAmount}}"]}");
        
        if (booking.PlatformFee.HasValue && booking.PlatformFee.Value > 0)
            sb.AppendLine($"Service Fee: {placeholders["{{currency}}"]} {placeholders["{{platformFee}}"]}");
        
        if (booking.DepositAmount > 0)
            sb.AppendLine($"Security Deposit (Refundable): {placeholders["{{currency}}"]} {placeholders["{{depositAmount}}"]}");
        
        if (booking.PromoDiscountAmount.HasValue && booking.PromoDiscountAmount.Value > 0)
            sb.AppendLine($"Promo Discount: -{placeholders["{{currency}}"]} {placeholders["{{discountAmount}}"]}");
        
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine($"TOTAL AMOUNT: {placeholders["{{currency}}"]} {placeholders["{{totalAmount}}"]}");
        sb.AppendLine();
        sb.AppendLine($"Payment Status: {placeholders["{{paymentStatus}}"].ToUpper()}");
        sb.AppendLine($"Payment Method: {placeholders["{{paymentMethod}}"]}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════");
        sb.AppendLine($"Thank you for choosing {template?.CompanyName ?? "RyvePool"}!");
        sb.AppendLine("═══════════════════════════════════════════════");
        
        return sb.ToString();
    }

    private Dictionary<string, string> BuildPlaceholders(Booking booking, Dictionary<string, string>? additionalPlaceholders)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["{{receiptNumber}}"] = $"RCT-{booking.CreatedAt.Year}-{booking.Id.ToString().Substring(0, 8).ToUpper()}",
            ["{{receiptDate}}"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["{{bookingReference}}"] = booking.BookingReference ?? "",
            ["{{customerName}}"] = $"{booking.Renter?.FirstName} {booking.Renter?.LastName}".Trim(),
            ["{{customerEmail}}"] = booking.Renter?.Email ?? "",
            ["{{customerPhone}}"] = booking.Renter?.Phone ?? "",
            ["{{pickupDateTime}}"] = booking.PickupDateTime.ToString("yyyy-MM-dd HH:mm"),
            ["{{returnDateTime}}"] = booking.ReturnDateTime.ToString("yyyy-MM-dd HH:mm"),
            ["{{totalDays}}"] = Math.Max(1, (booking.ReturnDateTime.Date - booking.PickupDateTime.Date).Days).ToString(),
            ["{{vehicleName}}"] = $"{booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}".Trim(),
            ["{{plateNumber}}"] = booking.Vehicle?.PlateNumber ?? "",
            ["{{currency}}"] = booking.Currency ?? "GHS",
            ["{{vehicleAmount}}"] = booking.RentalAmount.ToString("F2"),
            ["{{driverAmount}}"] = (booking.DriverAmount ?? 0m).ToString("F2"),
            ["{{protectionAmount}}"] = (booking.ProtectionAmount ?? 0m).ToString("F2"),
            ["{{platformFee}}"] = (booking.PlatformFee ?? 0m).ToString("F2"),
            ["{{depositAmount}}"] = booking.DepositAmount.ToString("F2"),
            ["{{discountAmount}}"] = (booking.PromoDiscountAmount ?? 0m).ToString("F2"),
            ["{{totalAmount}}"] = booking.TotalAmount.ToString("F2"),
            ["{{paymentStatus}}"] = booking.PaymentStatus ?? "",
            ["{{paymentMethod}}"] = booking.PaymentMethod ?? ""
        };

        if (additionalPlaceholders != null)
        {
            foreach (var kvp in additionalPlaceholders)
            {
                placeholders[kvp.Key] = kvp.Value;
            }
        }

        return placeholders;
    }

    private string GetDefaultCss()
    {
        return @"
            body {
                font-family: 'Segoe UI', Arial, sans-serif;
                margin: 0;
                padding: 20px;
                background-color: #f5f5f5;
            }
            .receipt-container {
                max-width: 800px;
                margin: 0 auto;
                background: white;
                padding: 40px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            }
            .header {
                text-align: center;
                border-bottom: 3px solid #2d7d5d;
                padding-bottom: 20px;
                margin-bottom: 30px;
            }
            .logo {
                max-width: 200px;
                height: auto;
                margin-bottom: 10px;
            }
            h1 {
                color: #2d7d5d;
                margin: 10px 0;
                font-size: 32px;
            }
            .company-info {
                color: #666;
                font-size: 14px;
                line-height: 1.6;
            }
            .receipt-title {
                text-align: center;
                margin-bottom: 30px;
            }
            .receipt-title h2 {
                color: #333;
                margin: 0;
                font-size: 28px;
            }
            .receipt-number {
                color: #666;
                margin: 10px 0 5px 0;
                font-size: 16px;
            }
            .date {
                color: #999;
                margin: 0;
                font-size: 14px;
            }
            .section {
                margin: 25px 0;
                padding: 20px;
                background: #f9f9f9;
                border-left: 4px solid #2d7d5d;
            }
            .section h3 {
                color: #2d7d5d;
                margin: 0 0 15px 0;
                font-size: 18px;
            }
            .section p {
                margin: 8px 0;
                color: #333;
            }
            .pricing-table {
                width: 100%;
                margin-top: 10px;
            }
            .pricing-table td {
                padding: 10px 0;
                border-bottom: 1px solid #ddd;
            }
            .pricing-table .amount {
                text-align: right;
                font-weight: 500;
            }
            .pricing-table .total {
                background: #f0f0f0;
                font-size: 18px;
            }
            .pricing-table .total td {
                padding: 15px 0;
                border-top: 2px solid #2d7d5d;
                border-bottom: none;
            }
            .status-paid {
                color: #28a745;
                font-weight: bold;
                text-transform: uppercase;
            }
            .status-pending {
                color: #ffc107;
                font-weight: bold;
                text-transform: uppercase;
            }
            .status-refunded {
                color: #17a2b8;
                font-weight: bold;
                text-transform: uppercase;
            }
            .footer {
                margin-top: 40px;
                padding-top: 20px;
                border-top: 2px solid #eee;
                text-align: center;
                color: #666;
            }
            .terms {
                font-size: 12px;
                line-height: 1.6;
                color: #999;
            }
            .generated {
                font-size: 11px;
                color: #ccc;
                margin-top: 20px;
            }
            @media print {
                body {
                    background: white;
                }
                .receipt-container {
                    box-shadow: none;
                }
            }
        ";
    }

    private ReceiptTemplate GetDefaultTemplate()
    {
        return new ReceiptTemplate
        {
            TemplateName = "default_receipt",
            LogoUrl = "https://i.imgur.com/ryvepool-logo.png",
            CompanyName = "RyvePool",
            CompanyAddress = "Accra, Ghana",
            CompanyPhone = "+233 XX XXX XXXX",
            CompanyEmail = "support@ryvepool.com",
            CompanyWebsite = "www.ryvepool.com",
            IsActive = true,
            ShowLogo = true,
            ReceiptNumberPrefix = "RCT",
            TermsAndConditions = "All rentals are subject to our terms and conditions. Prices include applicable taxes. For support, contact us at support@ryvepool.com"
        };
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Booking booking, Dictionary<string, string>? additionalPlaceholders = null)
    {
        try
        {
            var template = await GetActiveTemplateAsync();
            var placeholders = BuildPlaceholders(booking, additionalPlaceholders);

            // Use QuestPDF to generate a professional PDF
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text(template?.CompanyName ?? "RyvePool")
                            .Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                        
                        column.Item().AlignCenter().Text(template?.CompanyAddress ?? "Accra, Ghana")
                            .FontSize(9);
                        
                        column.Item().AlignCenter().Text($"{template?.CompanyPhone ?? ""} | {template?.CompanyEmail ?? ""}")
                            .FontSize(9);
                        
                        column.Item().PaddingTop(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Title
                        column.Item().PaddingBottom(10).Text("RENTAL RECEIPT").Bold().FontSize(16).FontColor(Colors.Blue.Darken1);

                        // Receipt details
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Receipt #: {placeholders["{{receiptNumber}}"]}").FontSize(10);
                            row.RelativeItem().AlignRight().Text($"Date: {placeholders["{{receiptDate}}"]}").FontSize(10);
                        });

                        column.Item().PaddingTop(5).Text($"Booking Ref: {placeholders["{{bookingReference}}"]}").FontSize(10);

                        // Customer Information
                        column.Item().PaddingTop(15).Text("Customer Information").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).PaddingLeft(10).Column(infoColumn =>
                        {
                            infoColumn.Item().Text($"Name: {placeholders["{{customerName}}"]}");
                            infoColumn.Item().Text($"Email: {placeholders["{{customerEmail}}"]}");
                            infoColumn.Item().Text($"Phone: {placeholders["{{customerPhone}}"]}");
                        });

                        // Rental Period
                        column.Item().PaddingTop(15).Text("Rental Period").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).PaddingLeft(10).Column(periodColumn =>
                        {
                            periodColumn.Item().Text($"Pickup: {placeholders["{{pickupDateTime}}"]}");
                            periodColumn.Item().Text($"Return: {placeholders["{{returnDateTime}}"]}");
                            periodColumn.Item().Text($"Duration: {placeholders["{{totalDays}}"]} day(s)");
                        });

                        // Vehicle Information
                        column.Item().PaddingTop(15).Text("Vehicle Information").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).PaddingLeft(10).Column(vehicleColumn =>
                        {
                            vehicleColumn.Item().Text($"Vehicle: {placeholders["{{vehicleName}}"]}");
                            vehicleColumn.Item().Text($"Plate Number: {placeholders["{{plateNumber}}"]}");
                        });

                        // Pricing Table
                        column.Item().PaddingTop(15).Text("Pricing Breakdown").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Description").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                            });

                            // Rows
                            table.Cell().Element(CellStyle).Text($"Vehicle ({placeholders["{{totalDays}}"]} day(s))");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{vehicleAmount}}"]}");

                            if (booking.WithDriver && booking.DriverAmount.HasValue && booking.DriverAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text($"Driver Service ({placeholders["{{totalDays}}"]} day(s))");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{driverAmount}}"]}");
                            }

                            if (booking.ProtectionAmount.HasValue && booking.ProtectionAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Protection Plan");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{protectionAmount}}"]}");
                            }

                            if (booking.PlatformFee.HasValue && booking.PlatformFee.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Service Fee");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{platformFee}}"]}");
                            }

                            if (booking.DepositAmount > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Security Deposit (Refundable):");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{depositAmount}}"]}");
                            }

                            if (booking.PromoDiscountAmount.HasValue && booking.PromoDiscountAmount.Value > 0)
                            {
                                table.Cell().Element(CellStyle).Text("Promo Discount");
                                table.Cell().Element(CellStyle).AlignRight().Text($"-{placeholders["{{currency}}"]} {placeholders["{{discountAmount}}"]}");
                            }

                            // Total row
                            table.Cell().Element(CellStyleBold).BorderTop(1).PaddingTop(5).Text("TOTAL");
                            table.Cell().Element(CellStyleBold).BorderTop(1).PaddingTop(5).AlignRight().Text($"{placeholders["{{currency}}"]} {placeholders["{{totalAmount}}"]}");
                        });

                        // Payment Status
                        column.Item().PaddingTop(15).Text("Payment Information").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).PaddingLeft(10).Column(paymentColumn =>
                        {
                            paymentColumn.Item().Text($"Status: {placeholders["{{paymentStatus}}"]}");
                            paymentColumn.Item().Text($"Method: {placeholders["{{paymentMethod}}"]}");
                        });

                        // Terms
                        if (!string.IsNullOrWhiteSpace(template?.TermsAndConditions))
                        {
                            column.Item().PaddingTop(20).PaddingHorizontal(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingTop(10).Text(template.TermsAndConditions).FontSize(8).Italic();
                        }
                    });

                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().Text($"Thank you for choosing {template?.CompanyName ?? "RyvePool"}!").FontSize(10).Bold();
                        column.Item().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF receipt for booking {BookingId}", booking.Id);
            throw;
        }
    }

    public async Task<byte[]> GenerateRentalAgreementPdfAsync(Booking booking, RentalAgreementAcceptance acceptance)
    {
        try
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().Element(ComposeRentalAgreementHeader);

                    // Content
                    page.Content().Column(column =>
                    {
                        column.Spacing(15);

                        // Agreement Info
                        column.Item().Text(text =>
                        {
                            text.Span("Agreement Date: ").Bold();
                            text.Span(acceptance.AcceptedAt.ToString("MMM dd, yyyy HH:mm"));
                        });
                        column.Item().Text(text =>
                        {
                            text.Span("Booking Reference: ").Bold();
                            text.Span(booking.BookingReference);
                        });
                        column.Item().Text(text =>
                        {
                            text.Span("Template Version: ").Bold();
                            text.Span($"{acceptance.TemplateCode} v{acceptance.TemplateVersion}");
                        });

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Renter Information
                        column.Item().PaddingTop(10).Text("RENTER INFORMATION").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text($"Name: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
                        column.Item().Text($"Email: {booking.Renter?.Email}");
                        column.Item().Text($"Phone: {booking.Renter?.Phone}");

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Vehicle Information
                        column.Item().PaddingTop(10).Text("VEHICLE INFORMATION").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text($"Vehicle: {booking.Vehicle?.Make} {booking.Vehicle?.Model} {booking.Vehicle?.Year}");
                        column.Item().Text($"Plate Number: {booking.Vehicle?.PlateNumber}");

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Rental Period
                        column.Item().PaddingTop(10).Text("RENTAL PERIOD").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text($"Pickup: {booking.PickupDateTime:MMM dd, yyyy HH:mm}");
                        column.Item().Text($"Return: {booking.ReturnDateTime:MMM dd, yyyy HH:mm}");

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Agreement Terms
                        column.Item().PaddingTop(10).Text("AGREEMENT TERMS").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text(acceptance.AgreementSnapshot ?? "[Agreement content]").FontSize(9);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Acceptance Confirmations
                        column.Item().PaddingTop(10).Text("ACCEPTANCE CONFIRMATIONS").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text($"✓ No Smoking Policy: {(acceptance.AcceptedNoSmoking ? "ACCEPTED" : "NOT ACCEPTED")}");
                        column.Item().Text($"✓ Fines & Tickets Responsibility: {(acceptance.AcceptedFinesAndTickets ? "ACCEPTED" : "NOT ACCEPTED")}");
                        column.Item().Text($"✓ Accident Procedure: {(acceptance.AcceptedAccidentProcedure ? "ACCEPTED" : "NOT ACCEPTED")}");

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Digital Signature
                        column.Item().PaddingTop(10).Text("DIGITAL SIGNATURE").Bold().FontSize(12);
                        column.Item().PaddingTop(5).Text($"Signed By: {booking.Renter?.FirstName} {booking.Renter?.LastName}");
                        column.Item().Text($"Date & Time: {acceptance.AcceptedAt:yyyy-MM-dd HH:mm:ss} UTC");
                        column.Item().Text($"IP Address: {acceptance.IpAddress}");
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate rental agreement PDF for booking {BookingId}", booking.Id);
            throw;
        }
    }

    private static void ComposeRentalAgreementHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text("VEHICLE RENTAL AGREEMENT").Bold().FontSize(16);
            column.Item().AlignCenter().Text("RYVE RENTAL").FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Medium);
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }

    private static IContainer CellStyleBold(IContainer container)
    {
        return container.PaddingVertical(5).DefaultTextStyle(x => x.Bold());
    }
}
