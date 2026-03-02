using LicenseManagement.Domain.Common;
using System.Net;

namespace LicenseManagement.Domain.Entities;

public class LicenseActivation : BaseEntity
{
    public Guid UserLicenseId { get; set; }
    public string HardwareId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public UserLicense UserLicense { get; set; } = null!;
}
