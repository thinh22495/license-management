using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Queries;

public class GetLicenseActivationsQuery : IRequest<ApiResponse<List<LicenseActivationDto>>>
{
    public Guid LicenseId { get; set; }
    public Guid UserId { get; set; }
}

public class GetLicenseActivationsQueryHandler : IRequestHandler<GetLicenseActivationsQuery, ApiResponse<List<LicenseActivationDto>>>
{
    private readonly IAppDbContext _context;

    public GetLicenseActivationsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<List<LicenseActivationDto>>> Handle(GetLicenseActivationsQuery request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .FirstOrDefaultAsync(ul => ul.Id == request.LicenseId && ul.UserId == request.UserId, ct);

        if (license == null)
            return ApiResponse<List<LicenseActivationDto>>.Fail("License không tồn tại hoặc bạn không có quyền truy cập");

        var activations = await _context.LicenseActivations
            .Where(a => a.UserLicenseId == request.LicenseId)
            .OrderByDescending(a => a.LastSeenAt)
            .Select(a => new LicenseActivationDto
            {
                Id = a.Id,
                HardwareId = a.HardwareId,
                MachineName = a.MachineName,
                IpAddress = a.IpAddress,
                ActivatedAt = a.ActivatedAt,
                LastSeenAt = a.LastSeenAt,
                IsActive = a.IsActive,
            })
            .ToListAsync(ct);

        return ApiResponse<List<LicenseActivationDto>>.Ok(activations);
    }
}
