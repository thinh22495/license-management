using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Notifications.Commands;

public class AdminDeleteNotificationCommand : IRequest<ApiResponse>
{
    public Guid NotificationId { get; set; }
}

public class AdminDeleteNotificationCommandHandler : IRequestHandler<AdminDeleteNotificationCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public AdminDeleteNotificationCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(AdminDeleteNotificationCommand request, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, ct);

        if (notification == null)
            return ApiResponse.Fail("Không tìm thấy thông báo");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok("Đã xóa thông báo");
    }
}
