using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;

namespace GhanaHybridRentalApi.Services;

public interface IEmailTemplateService
{
    Task<EmailTemplate?> GetTemplateAsync(string templateName);
    Task<List<EmailTemplate>> GetAllTemplatesAsync();
    Task<EmailTemplate> CreateOrUpdateTemplateAsync(EmailTemplate template);
    Task<bool> DeleteTemplateAsync(Guid templateId);
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> placeholders);
    Task<string> RenderSubjectAsync(string templateName, Dictionary<string, string> placeholders);
}

public class EmailTemplateService : IEmailTemplateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(AppDbContext db, ILogger<EmailTemplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EmailTemplate?> GetTemplateAsync(string templateName)
    {
        return await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateName == templateName && t.IsActive);
    }

    public async Task<List<EmailTemplate>> GetAllTemplatesAsync()
    {
        return await _db.EmailTemplates
            .OrderBy(t => t.Category)
            .ThenBy(t => t.TemplateName)
            .ToListAsync();
    }

    public async Task<EmailTemplate> CreateOrUpdateTemplateAsync(EmailTemplate template)
    {
        var existing = await _db.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateName == template.TemplateName);

        if (existing != null)
        {
            // Update existing
            existing.Subject = template.Subject;
            existing.BodyTemplate = template.BodyTemplate;
            existing.Description = template.Description;
            existing.Category = template.Category;
            existing.IsActive = template.IsActive;
            existing.IsHtml = template.IsHtml;
            existing.AvailablePlaceholdersJson = template.AvailablePlaceholdersJson;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }
        else
        {
            // Create new
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _db.EmailTemplates.Add(template);
            await _db.SaveChangesAsync();
            return template;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        var template = await _db.EmailTemplates.FindAsync(templateId);
        if (template == null)
            return false;

        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> placeholders)
    {
        var template = await GetTemplateAsync(templateName);
        
        if (template == null)
        {
            _logger.LogWarning("Template '{TemplateName}' not found, using default", templateName);
            return string.Join("\n", placeholders.Select(p => $"{p.Key}: {p.Value}"));
        }

        var rendered = template.BodyTemplate;

        // Replace all {{placeholder}} with actual values
        foreach (var placeholder in placeholders)
        {
            var pattern = $"{{{{{placeholder.Key}}}}}"; // Matches {{key}}
            rendered = rendered.Replace(pattern, placeholder.Value);
        }

        // Remove any unreplaced placeholders (optional - or leave them for debugging)
        rendered = Regex.Replace(rendered, @"\{\{[^}]+\}\}", string.Empty);

        return rendered;
    }

    public async Task<string> RenderSubjectAsync(string templateName, Dictionary<string, string> placeholders)
    {
        var template = await GetTemplateAsync(templateName);
        
        if (template == null)
        {
            return $"Notification - {DateTime.UtcNow:yyyy-MM-dd}";
        }

        var rendered = template.Subject;

        foreach (var placeholder in placeholders)
        {
            var pattern = $"{{{{{placeholder.Key}}}}}";
            rendered = rendered.Replace(pattern, placeholder.Value);
        }

        return rendered;
    }
}
