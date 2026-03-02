using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class RevokeLicenseCommand : IRequest<ApiResponse>
{
    public Guid LicenseId { get; set; }
    public string Action { get; set; } = "revoke"; // revoke, suspend, reinstate
}

public class RevokeLicenseCommandHandler : IRequestHandler<RevokeLicenseCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public RevokeLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(RevokeLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses.FirstOrDefaultAsync(ul => ul.Id == request.LicenseId, ct);
        if (license == null)
            return ApiResponse.Fail("License không tồn tại");

        var (newStatus, message) = request.Action.ToLowerInvariant() switch
        {
            "revoke" => (LicenseStatus.Revoked, "Đã thu hồi license"),
            "suspend" => (LicenseStatus.Suspended, "Đã tạm dừng license"),
            "reinstate" => (LicenseStatus.Active, "Đã khôi phục license"),
            _ => (license.Status, "Hành động không hợp lệ")
        };

        if (newStatus == license.Status && request.Action != "reinstate")
            return ApiResponse.Fail("Hành động không hợp lệ");

        license.Status = newStatus;

        _context.LicenseEvents.Add(new LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = request.Action,
        });

        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok(message);
    }
}
