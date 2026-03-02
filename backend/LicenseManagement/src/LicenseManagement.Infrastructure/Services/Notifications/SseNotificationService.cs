using System.Collections.Concurrent;
using LicenseManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Services.Notifications;

public class SseNotificationService : ISseNotificationService
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<StreamWriter>> _clients = new();
    private readonly ILogger<SseNotificationService> _logger;

    public SseNotificationService(ILogger<SseNotificationService> logger)
    {
        _logger = logger;
    }

    public void AddClient(Guid userId, StreamWriter writer)
    {
        var bag = _clients.GetOrAdd(userId, _ => new ConcurrentBag<StreamWriter>());
        bag.Add(writer);
        _logger.LogInformation("SSE client connected: User={UserId}, Total={Count}", userId, bag.Count);
    }

    public void RemoveClient(Guid userId, StreamWriter writer)
    {
        if (_clients.TryGetValue(userId, out var bag))
        {
            var remaining = new ConcurrentBag<StreamWriter>(bag.Where(w => w != writer));
            _clients.TryUpdate(userId, remaining, bag);

            if (remaining.IsEmpty)
                _clients.TryRemove(userId, out _);
        }
    }

    public async Task SendToUserAsync(Guid userId, string eventType, string data)
    {
        if (!_clients.TryGetValue(userId, out var writers))
            return;

        var deadWriters = new List<StreamWriter>();

        foreach (var writer in writers)
        {
            try
            {
                await writer.WriteLineAsync($"event: {eventType}");
                await writer.WriteLineAsync($"data: {data}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
            catch
            {
                deadWriters.Add(writer);
            }
        }

        // Cleanup dead connections
        if (deadWriters.Count > 0)
        {
            var remaining = new ConcurrentBag<StreamWriter>(writers.Where(w => !deadWriters.Contains(w)));
            _clients.TryUpdate(userId, remaining, writers);
        }
    }

    public async Task BroadcastAsync(string eventType, string data)
    {
        foreach (var userId in _clients.Keys)
        {
            await SendToUserAsync(userId, eventType, data);
        }
    }
}
