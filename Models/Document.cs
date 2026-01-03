using System;

namespace GhanaHybridRentalApi.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid? UploadedByUserId { get; set; }
}
