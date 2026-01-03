using Microsoft.AspNetCore.Mvc;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class EmailTemplateEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        // Get all email templates
        app.MapGet("/api/v1/admin/email-templates", GetAllTemplatesAsync)
            .RequireAuthorization("AdminOnly");

        // Get single template by name
        app.MapGet("/api/v1/admin/email-templates/{templateName}", GetTemplateByNameAsync)
            .RequireAuthorization("AdminOnly");

        // Create or update template
        app.MapPost("/api/v1/admin/email-templates", CreateOrUpdateTemplateAsync)
            .RequireAuthorization("AdminOnly");

        // Delete template
        app.MapDelete("/api/v1/admin/email-templates/{templateId:guid}", DeleteTemplateAsync)
            .RequireAuthorization("AdminOnly");

        // Get available placeholder info for a template type
        app.MapGet("/api/v1/admin/email-templates/placeholders/{templateType}", GetAvailablePlaceholdersAsync)
            .RequireAuthorization("AdminOnly");

        // Test render a template
        app.MapPost("/api/v1/admin/email-templates/{templateName}/preview", PreviewTemplateAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetAllTemplatesAsync(
        IEmailTemplateService templateService)
    {
        var templates = await templateService.GetAllTemplatesAsync();
        return Results.Ok(new { templates });
    }

    private static async Task<IResult> GetTemplateByNameAsync(
        string templateName,
        IEmailTemplateService templateService)
    {
        var template = await templateService.GetTemplateAsync(templateName);
        
        if (template == null)
            return Results.NotFound(new { error = $"Template '{templateName}' not found" });

        return Results.Ok(template);
    }

    private static async Task<IResult> CreateOrUpdateTemplateAsync(
        [FromBody] EmailTemplateRequest request,
        IEmailTemplateService templateService,
        AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName))
            return Results.BadRequest(new { error = "Template name is required" });

        if (string.IsNullOrWhiteSpace(request.Subject))
            return Results.BadRequest(new { error = "Subject is required" });

        if (string.IsNullOrWhiteSpace(request.BodyTemplate))
            return Results.BadRequest(new { error = "Body template is required" });

        var template = new EmailTemplate
        {
            TemplateName = request.TemplateName,
            Subject = request.Subject,
            BodyTemplate = request.BodyTemplate,
            Description = request.Description,
            Category = request.Category ?? "general",
            IsActive = request.IsActive ?? true,
            IsHtml = request.IsHtml ?? false,
            AvailablePlaceholdersJson = request.AvailablePlaceholdersJson
        };

        var result = await templateService.CreateOrUpdateTemplateAsync(template);
        
        return Results.Ok(new 
        { 
            success = true, 
            message = "Template saved successfully",
            template = result 
        });
    }

    private static async Task<IResult> DeleteTemplateAsync(
        Guid templateId,
        IEmailTemplateService templateService)
    {
        var deleted = await templateService.DeleteTemplateAsync(templateId);
        
        if (!deleted)
            return Results.NotFound(new { error = "Template not found" });

        return Results.Ok(new { success = true, message = "Template deleted successfully" });
    }

    private static IResult GetAvailablePlaceholdersAsync(
        string templateType)
    {
        // Return available placeholders based on template type
        var placeholders = templateType.ToLower() switch
        {
            "booking_confirmation" => new[]
            {
                new { key = "customerName", description = "Customer's full name", example = "John Doe" },
                new { key = "bookingRef", description = "Booking reference number", example = "BKG-2025-001" },
                new { key = "vehicleName", description = "Vehicle make and model", example = "Toyota Camry 2023" },
                new { key = "pickupDate", description = "Pickup date and time", example = "Dec 25, 2025 @ 10:00 AM" },
                new { key = "returnDate", description = "Return date and time", example = "Dec 30, 2025 @ 10:00 AM" },
                new { key = "totalAmount", description = "Total booking amount", example = "GHS 1,500.00" },
                new { key = "currency", description = "Currency code", example = "GHS" },
                new { key = "pickupLocation", description = "Pickup location", example = "Kotoka Airport" },
                new { key = "returnLocation", description = "Return location", example = "Kotoka Airport" },
                new { key = "withDriver", description = "With driver or self-drive", example = "Self-Drive" },
                new { key = "driverName", description = "Driver name (if applicable)", example = "Michael Smith" },
                new { key = "plateNumber", description = "Vehicle plate number", example = "GH 1234-20" },
                new { key = "supportPhone", description = "Support phone number", example = "+233 XX XXX XXXX" },
                new { key = "supportEmail", description = "Support email", example = "support@ryverental.com" }
            },
            "cancellation" => new[]
            {
                new { key = "customerName", description = "Customer's full name", example = "John Doe" },
                new { key = "bookingRef", description = "Booking reference number", example = "BKG-2025-001" },
                new { key = "vehicleName", description = "Vehicle make and model", example = "Toyota Camry 2023" },
                new { key = "cancellationReason", description = "Reason for cancellation", example = "Customer request" },
                new { key = "refundAmount", description = "Refund amount if applicable", example = "GHS 500.00" },
                new { key = "supportPhone", description = "Support phone number", example = "+233 XX XXX XXXX" }
            },
            "verification" => new[]
            {
                new { key = "customerName", description = "Customer's full name", example = "John Doe" },
                new { key = "verificationLink", description = "Email verification link", example = "https://..." },
                new { key = "expiryHours", description = "Link expiry time in hours", example = "24" }
            },
            "payout" => new[]
            {
                new { key = "ownerName", description = "Owner's full name", example = "Jane Smith" },
                new { key = "payoutAmount", description = "Payout amount", example = "GHS 2,500.00" },
                new { key = "currency", description = "Currency code", example = "GHS" },
                new { key = "payoutDate", description = "Expected payout date", example = "Dec 28, 2025" },
                new { key = "paymentMethod", description = "Payout method", example = "Mobile Money" },
                new { key = "accountDetails", description = "Account/phone number", example = "+233 XX XXX XXXX" }
            },
            _ => new[]
            {
                new { key = "customerName", description = "Recipient's name", example = "John Doe" },
                new { key = "message", description = "Message content", example = "Your message here" },
                new { key = "supportEmail", description = "Support email", example = "support@ryverental.com" }
            }
        };

        return Results.Ok(new { templateType, placeholders });
    }

    private static async Task<IResult> PreviewTemplateAsync(
        string templateName,
        [FromBody] PreviewRequest request,
        IEmailTemplateService templateService)
    {
        var placeholders = request.Placeholders ?? new Dictionary<string, string>();
        
        // Add some default values if not provided
        if (!placeholders.ContainsKey("customerName"))
            placeholders["customerName"] = "John Doe";
        if (!placeholders.ContainsKey("supportEmail"))
            placeholders["supportEmail"] = "support@ryverental.com";

        var renderedBody = await templateService.RenderTemplateAsync(templateName, placeholders);
        var renderedSubject = await templateService.RenderSubjectAsync(templateName, placeholders);

        return Results.Ok(new 
        { 
            subject = renderedSubject,
            body = renderedBody,
            placeholders = placeholders
        });
    }
}

public record EmailTemplateRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsHtml { get; set; }
    public string? AvailablePlaceholdersJson { get; set; }
}

public record PreviewRequest
{
    public Dictionary<string, string>? Placeholders { get; set; }
}
