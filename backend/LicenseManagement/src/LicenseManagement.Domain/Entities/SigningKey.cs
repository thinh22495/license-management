using LicenseManagement.Domain.Common;

namespace LicenseManagement.Domain.Entities;

public class SigningKey : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Algorithm { get; set; } = "Ed25519";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKeyEnc { get; set; } = string.Empty; // AES-256-GCM encrypted
    public bool IsActive { get; set; } = true;
    public DateTime? RotatedAt { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}
