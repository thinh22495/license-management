namespace LicenseManagement.Application.Common.Interfaces;

public interface ISseNotificationService
{
    void AddClient(Guid userId, StreamWriter writer);
    void RemoveClient(Guid userId, StreamWriter writer);
    Task SendToUserAsync(Guid userId, string eventType, string data);
    Task BroadcastAsync(string eventType, string data);
}
