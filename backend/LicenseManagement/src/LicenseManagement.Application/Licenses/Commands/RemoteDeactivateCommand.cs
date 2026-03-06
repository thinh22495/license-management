using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class RemoteDeactivateCommand : IRequest<ApiResponse>
{
    public Guid LicenseId { get; set; }
    public Guid ActivationId { get; set; }
    public Guid UserId { get; set; }
}

public class RemoteDeactivateCommandHandler : IRequestHandler<RemoteDeactivateCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public RemoteDeactivateCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(RemoteDeactivateCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.Activations)
            .FirstOrDefaultAsync(ul => ul.Id == request.LicenseId && ul.UserId == request.UserId, ct);

        if (license == null)
            return ApiResponse.Fail("License không tồn tại hoặc bạn không có quyền truy cập");

        var activation = license.Activations.FirstOrDefault(a => a.Id == request.ActivationId && a.IsActive);
        if (activation == null)
            return ApiResponse.Fail("Không tìm thấy kích hoạt hoặc đã bị hủy");

        activation.IsActive = false;
        license.CurrentActivations = license.Activations.Count(a => a.IsActive);

        _context.LicenseEvents.Add(new LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = "deactivated",
            Details = System.Text.Json.JsonSerializer.Serialize(new
            {
                hardwareId = activation.HardwareId,
                machineName = activation.MachineName,
                source = "web_portal"
            }),
        });

        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok("Đã hủy kích hoạt thiết bị từ xa");
    }
}
