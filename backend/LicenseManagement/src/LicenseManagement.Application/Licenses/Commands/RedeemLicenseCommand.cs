using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class RedeemLicenseCommand : IRequest<ApiResponse<LicenseDto>>
{
    public string LicenseKey { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

public class RedeemLicenseCommandHandler : IRequestHandler<RedeemLicenseCommand, ApiResponse<LicenseDto>>
{
    private readonly IAppDbContext _context;

    public RedeemLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicenseDto>> Handle(RedeemLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .FirstOrDefaultAsync(ul => ul.LicenseKey == request.LicenseKey, ct);

        if (license == null)
            return ApiResponse<LicenseDto>.Fail("License key không hợp lệ");

        if (license.Status != LicenseStatus.Pending || license.UserId != null)
            return ApiResponse<LicenseDto>.Fail("License key này không thể nhập. Key đã được sử dụng hoặc không hợp lệ");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user == null)
            return ApiResponse<LicenseDto>.Fail("Người dùng không tồn tại");

        license.UserId = request.UserId;
        license.Status = LicenseStatus.Active;

        // Set expiry from now if duration-based
        if (license.LicenseProduct.DurationDays > 0)
            license.ExpiresAt = DateTime.UtcNow.AddDays(license.LicenseProduct.DurationDays);

        _context.LicenseEvents.Add(new Domain.Entities.LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = "redeemed",
            Details = System.Text.Json.JsonSerializer.Serialize(new { userId = request.UserId }),
        });

        await _context.SaveChangesAsync(ct);

        return ApiResponse<LicenseDto>.Ok(new LicenseDto
        {
            Id = license.Id,
            UserId = license.UserId,
            UserEmail = user.Email,
            ProductName = license.LicenseProduct.Product.Name,
            PlanName = license.LicenseProduct.Name,
            LicenseKey = license.LicenseKey,
            Status = license.Status.ToString(),
            ExpiresAt = license.ExpiresAt,
            CurrentActivations = 0,
            MaxActivations = license.LicenseProduct.MaxActivations,
            CreatedAt = license.CreatedAt,
        });
    }
}
