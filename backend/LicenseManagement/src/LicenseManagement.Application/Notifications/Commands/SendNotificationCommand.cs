using System.Text.Json;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Notifications.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Commands;

public class SendNotificationCommand : IRequest<ApiResponse>
{
    public Guid? UserId { get; set; } // null = broadcast
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string[] Channels { get; set; } = ["web"]; // web, email
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, ApiResponse>
{
    private readonly IAppDbContext _context;
    private readonly ISseNotificationService _sseService;
    private readonly IEmailService _emailService;

    public SendNotificationCommandHandler(
        IAppDbContext context,
        ISseNotificationService sseService,
        IEmailService emailService)
    {
        _context = context;
        _sseService = sseService;
        _emailService = emailService;
    }

    public async Task<ApiResponse> Handle(SendNotificationCommand request, CancellationToken ct)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            Channels = request.Channels,
            SentAt = DateTime.UtcNow,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Body = notification.Body,
            Type = notification.Type.ToString(),
            IsRead = false,
            CreatedAt = notification.CreatedAt,
        };

        var json = JsonSerializer.Serialize(dto);

        // Send via SSE
        if (request.UserId.HasValue)
        {
            await _sseService.SendToUserAsync(request.UserId.Value, "notification", json);
        }
        else
        {
            await _sseService.BroadcastAsync("notification", json);
        }

        // Send email if channel includes "email"
        if (request.Channels.Contains("email"))
        {
            if (request.UserId.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId.Value, ct);
                if (user != null)
                {
                    await _emailService.SendAsync(user.Email, request.Title, $"<h3>{request.Title}</h3><p>{request.Body}</p>", ct);
                }
            }
        }

        return ApiResponse.Ok("Đã gửi thông báo");
    }
}
