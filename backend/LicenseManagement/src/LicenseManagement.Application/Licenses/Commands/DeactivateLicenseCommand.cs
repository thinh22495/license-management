using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class DeactivateLicenseCommand : IRequest<ApiResponse>
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class DeactivateLicenseCommandHandler : IRequestHandler<DeactivateLicenseCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public DeactivateLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(DeactivateLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.Activations)
            .FirstOrDefaultAsync(ul => ul.LicenseKey == request.LicenseKey, ct);

        if (license == null)
            return ApiResponse.Fail("License key không hợp lệ");

        var activation = license.Activations
            .FirstOrDefault(a => a.HardwareId == request.HardwareId && a.IsActive);

        if (activation == null)
            return ApiResponse.Fail("Không tìm thấy kích hoạt cho thiết bị này");

        activation.IsActive = false;
        license.CurrentActivations = license.Activations.Count(a => a.IsActive);

        _context.LicenseEvents.Add(new LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = "deactivated",
            Details = System.Text.Json.JsonSerializer.Serialize(new { hardwareId = request.HardwareId }),
            IpAddress = request.IpAddress,
        });

        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok("Đã hủy kích hoạt thiết bị");
    }
}
