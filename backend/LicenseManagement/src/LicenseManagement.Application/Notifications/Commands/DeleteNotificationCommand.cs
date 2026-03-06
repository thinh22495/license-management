using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Commands;

public class DeleteNotificationCommand : IRequest<ApiResponse>
{
    public Guid UserId { get; set; }
    public Guid NotificationId { get; set; }
}

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public DeleteNotificationCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId
                && (n.UserId == request.UserId || n.UserId == null), ct);

        if (notification == null)
            return ApiResponse.Fail("Không tìm thấy thông báo");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok("Đã xóa thông báo");
    }
}
