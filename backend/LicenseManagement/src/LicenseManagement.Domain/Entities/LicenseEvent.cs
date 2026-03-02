namespace LicenseManagement.Domain.Entities;

public class LicenseEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserLicenseId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = "{}"; // JSONB
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserLicense UserLicense { get; set; } = null!;
}
