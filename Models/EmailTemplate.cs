using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class EmailTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string TemplateName { get; set; } = string.Empty; // booking_confirmation, cancellation, verification, etc.

    [Required]
    [MaxLength(256)]
    public string Subject { get; set; } = string.Empty; // Can include {{placeholders}}

    [Required]
    public string BodyTemplate { get; set; } = string.Empty; // HTML or plain text with {{placeholders}}

    public string? Description { get; set; }

    [MaxLength(50)]
    public string Category { get; set; } = "general"; // booking, notification, system

    public bool IsActive { get; set; } = true;

    public bool IsHtml { get; set; } = false; // true for HTML, false for plain text

    // Available placeholders as JSON array for UI reference
    public string? AvailablePlaceholdersJson { get; set; } // ["customerName", "bookingRef", "vehicleName", etc.]

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByUserId { get; set; }
}
