using LicenseManagement.Domain.Common;

namespace LicenseManagement.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string Metadata { get; set; } = "{}"; // JSONB

    // Navigation properties
    public ICollection<LicenseProduct> LicenseProducts { get; set; } = new List<LicenseProduct>();
    public ICollection<SigningKey> SigningKeys { get; set; } = new List<SigningKey>();
}
