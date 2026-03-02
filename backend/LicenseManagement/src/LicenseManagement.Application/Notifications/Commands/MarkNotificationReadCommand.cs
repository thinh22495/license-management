using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Commands;

public class MarkNotificationReadCommand : IRequest<ApiResponse>
{
    public Guid UserId { get; set; }
    public Guid? NotificationId { get; set; } // null = mark all as read
}

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public MarkNotificationReadCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        if (request.NotificationId.HasValue)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId.Value
                    && (n.UserId == request.UserId || n.UserId == null), ct);

            if (notification != null)
                notification.IsRead = true;
        }
        else
        {
            var notifications = await _context.Notifications
                .Where(n => (n.UserId == request.UserId || n.UserId == null) && !n.IsRead)
                .ToListAsync(ct);

            foreach (var n in notifications)
                n.IsRead = true;
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok("Đã đánh dấu đã đọc");
    }
}
