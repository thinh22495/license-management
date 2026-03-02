using LicenseManagement.Domain.Common;
using LicenseManagement.Domain.Enums;

namespace LicenseManagement.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; } // null = broadcast
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string[] Channels { get; set; } = ["web"];
    public bool IsRead { get; set; }
    public DateTime? SentAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
