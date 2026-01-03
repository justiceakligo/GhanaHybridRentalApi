using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class AppConfig
{
    [Key]
    [MaxLength(128)]
    public string ConfigKey { get; set; } = string.Empty;

    public string ConfigValue { get; set; } = string.Empty;

    public bool IsSensitive { get; set; } = false;

    [MaxLength(32)]
    public string Scope { get; set; } = "other"; // security, payments, integrations, other

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
