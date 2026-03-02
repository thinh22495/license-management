using LicenseManagement.Domain.Common;

namespace LicenseManagement.Domain.Entities;

public class LicenseProduct : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; } // 0 = lifetime
    public int MaxActivations { get; set; } = 1;
    public long Price { get; set; } // VND
    public string Features { get; set; } = "[]"; // JSONB
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Product Product { get; set; } = null!;
    public ICollection<UserLicense> UserLicenses { get; set; } = new List<UserLicense>();
}
