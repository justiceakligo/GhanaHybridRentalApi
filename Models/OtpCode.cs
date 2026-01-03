using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class OtpCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(32)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(16)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Purpose { get; set; } = "register"; // register | login | verify

    [MaxLength(32)]
    public string Channel { get; set; } = "whatsapp";

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
