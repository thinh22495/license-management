using LicenseManagement.Domain.Common;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Domain.Entities;

public class UserLicense : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid LicenseProductId { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // null = lifetime
    public int CurrentActivations { get; set; }
    public string Metadata { get; set; } = "{}"; // JSONB

    // Navigation properties
    public User? User { get; set; }
    public LicenseProduct LicenseProduct { get; set; } = null!;
    public ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();
    public ICollection<LicenseEvent> Events { get; set; } = new List<LicenseEvent>();
}
