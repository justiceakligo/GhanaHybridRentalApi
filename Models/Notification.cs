using System.ComponentModel.DataAnnotations;

namespace GhanaHybridRentalApi.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Read { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
