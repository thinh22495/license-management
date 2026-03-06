using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class AdminCreateLicenseCommand : IRequest<ApiResponse<LicenseDto>>
{
    public Guid? UserId { get; set; }
    public Guid LicensePlanId { get; set; }
    public string? Note { get; set; }
}

public class AdminCreateLicenseCommandHandler : IRequestHandler<AdminCreateLicenseCommand, ApiResponse<LicenseDto>>
{
    private readonly IAppDbContext _context;

    public AdminCreateLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicenseDto>> Handle(AdminCreateLicenseCommand request, CancellationToken ct)
    {
        var plan = await _context.LicenseProducts
            .Include(lp => lp.Product)
            .FirstOrDefaultAsync(lp => lp.Id == request.LicensePlanId && lp.IsActive, ct);

        if (plan == null)
            return ApiResponse<LicenseDto>.Fail("Gói license không tồn tại hoặc đã ngừng bán");

        User? user = null;
        if (request.UserId.HasValue)
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId.Value, ct);
            if (user == null)
                return ApiResponse<LicenseDto>.Fail("Người dùng không tồn tại");
        }

        var licenseKey = $"LM-{Guid.NewGuid():N}".ToUpperInvariant()[..24];

        // Only set expiry now if assigned to a user; pending keys get expiry on redeem
        var isAssigned = request.UserId.HasValue;
        var expiresAt = isAssigned && plan.DurationDays > 0
            ? DateTime.UtcNow.AddDays(plan.DurationDays)
            : (DateTime?)null;

        var license = new UserLicense
        {
            UserId = request.UserId,
            LicenseProductId = plan.Id,
            LicenseKey = licenseKey,
            Status = isAssigned ? LicenseStatus.Active : LicenseStatus.Pending,
            ExpiresAt = expiresAt,
            Metadata = string.IsNullOrEmpty(request.Note)
                ? "{}"
                : System.Text.Json.JsonSerializer.Serialize(new { note = request.Note, source = "admin_gift" })
        };

        _context.UserLicenses.Add(license);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<LicenseDto>.Ok(new LicenseDto
        {
            Id = license.Id,
            UserId = license.UserId,
            UserEmail = user?.Email ?? "",
            ProductName = plan.Product.Name,
            PlanName = plan.Name,
            LicenseKey = license.LicenseKey,
            Status = license.Status.ToString(),
            ExpiresAt = license.ExpiresAt,
            CurrentActivations = 0,
            MaxActivations = plan.MaxActivations,
            CreatedAt = license.CreatedAt,
        });
    }
}
