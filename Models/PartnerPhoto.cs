using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class PartnerPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    [MaxLength(512)]
    public string Url { get; set; } = string.Empty;
    [MaxLength(256)]
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsLogo { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Partner? Partner { get; set; }
}
