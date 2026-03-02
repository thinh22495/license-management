using LicenseManagement.Domain.Common;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public long Balance { get; set; }
    public bool IsLocked { get; set; }
    public bool EmailVerified { get; set; }
    public string? AvatarUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation properties
    public ICollection<UserLicense> Licenses { get; set; } = new List<UserLicense>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<PushSubscription> PushSubscriptions { get; set; } = new List<PushSubscription>();
}
