using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class ReceiptTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string TemplateName { get; set; } = string.Empty; // "default_receipt"

    [MaxLength(512)]
    public string LogoUrl { get; set; } = "https://i.imgur.com/ryvepool-logo.png"; // Company logo

    [Required]
    [MaxLength(256)]
    public string CompanyName { get; set; } = "RyvePool";

    [MaxLength(512)]
    public string CompanyAddress { get; set; } = "Accra, Ghana";

    [MaxLength(128)]
    public string CompanyPhone { get; set; } = "+233 XX XXX XXXX";

    [MaxLength(256)]
    public string CompanyEmail { get; set; } = "support@ryvepool.com";

    [MaxLength(512)]
    public string CompanyWebsite { get; set; } = "www.ryvepool.com";

    // Header section template
    public string HeaderTemplate { get; set; } = string.Empty;

    // Footer section template
    public string FooterTemplate { get; set; } = string.Empty;

    // Terms and conditions displayed at bottom
    public string? TermsAndConditions { get; set; }

    // Custom CSS styling for HTML receipts
    public string? CustomCss { get; set; }

    public bool IsActive { get; set; } = true;

    public bool ShowLogo { get; set; } = true;

    public bool ShowQrCode { get; set; } = false; // Future: QR code for receipt verification

    [MaxLength(50)]
    public string ReceiptNumberPrefix { get; set; } = "RCT"; // RCT-2026-XXXXXXXX

    // Available placeholders as JSON for UI reference
    public string? AvailablePlaceholdersJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; set; }
}
