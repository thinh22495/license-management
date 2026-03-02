using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class PurchaseLicenseCommand : IRequest<ApiResponse<LicenseDto>>
{
    public Guid UserId { get; set; }
    public Guid LicensePlanId { get; set; }
}

public class PurchaseLicenseCommandHandler : IRequestHandler<PurchaseLicenseCommand, ApiResponse<LicenseDto>>
{
    private readonly IAppDbContext _context;

    public PurchaseLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicenseDto>> Handle(PurchaseLicenseCommand request, CancellationToken ct)
    {
        var plan = await _context.LicenseProducts
            .Include(lp => lp.Product)
            .FirstOrDefaultAsync(lp => lp.Id == request.LicensePlanId && lp.IsActive, ct);

        if (plan == null)
            return ApiResponse<LicenseDto>.Fail("Gói license không tồn tại hoặc đã ngừng bán");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user == null)
            return ApiResponse<LicenseDto>.Fail("Người dùng không tồn tại");

        if (user.IsLocked)
            return ApiResponse<LicenseDto>.Fail("Tài khoản đã bị khóa");

        if (user.Balance < plan.Price)
            return ApiResponse<LicenseDto>.Fail($"Số dư không đủ. Cần {plan.Price:N0} VND, hiện có {user.Balance:N0} VND");

        // Generate license key (simple UUID-based key)
        var licenseKey = $"LM-{Guid.NewGuid():N}".ToUpperInvariant()[..24];

        var expiresAt = plan.DurationDays > 0
            ? DateTime.UtcNow.AddDays(plan.DurationDays)
            : (DateTime?)null;

        var license = new UserLicense
        {
            UserId = request.UserId,
            LicenseProductId = plan.Id,
            LicenseKey = licenseKey,
            Status = LicenseStatus.Active,
            ExpiresAt = expiresAt,
        };

        // Deduct balance
        var balanceBefore = user.Balance;
        user.Balance -= plan.Price;

        // Create transaction
        var transaction = new Transaction
        {
            UserId = request.UserId,
            Type = TransactionType.Purchase,
            Amount = plan.Price,
            BalanceBefore = balanceBefore,
            BalanceAfter = user.Balance,
            PaymentMethod = PaymentMethod.Balance,
            Status = TransactionStatus.Completed,
            RelatedLicenseId = license.Id,
        };

        _context.UserLicenses.Add(license);
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<LicenseDto>.Ok(new LicenseDto
        {
            Id = license.Id,
            UserId = license.UserId,
            UserEmail = user.Email,
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
