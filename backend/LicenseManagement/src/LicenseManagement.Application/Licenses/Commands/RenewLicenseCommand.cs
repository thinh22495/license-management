using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Commands;

public class RenewLicenseCommand : IRequest<ApiResponse<LicenseDto>>
{
    public Guid UserId { get; set; }
    public Guid LicenseId { get; set; }
}

public class RenewLicenseCommandHandler : IRequestHandler<RenewLicenseCommand, ApiResponse<LicenseDto>>
{
    private readonly IAppDbContext _context;

    public RenewLicenseCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicenseDto>> Handle(RenewLicenseCommand request, CancellationToken ct)
    {
        var license = await _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .FirstOrDefaultAsync(ul => ul.Id == request.LicenseId && ul.UserId == request.UserId, ct);

        if (license == null)
            return ApiResponse<LicenseDto>.Fail("License không tồn tại");

        if (license.LicenseProduct.DurationDays == 0)
            return ApiResponse<LicenseDto>.Fail("License vĩnh viễn không cần gia hạn");

        var plan = license.LicenseProduct;
        var user = license.User;

        if (user.Balance < plan.Price)
            return ApiResponse<LicenseDto>.Fail($"Số dư không đủ. Cần {plan.Price:N0} VND");

        // Extend expiry from current expiry or now (if expired)
        var baseDate = license.ExpiresAt.HasValue && license.ExpiresAt.Value > DateTime.UtcNow
            ? license.ExpiresAt.Value
            : DateTime.UtcNow;

        license.ExpiresAt = baseDate.AddDays(plan.DurationDays);
        license.Status = LicenseStatus.Active;

        var balanceBefore = user.Balance;
        user.Balance -= plan.Price;

        _context.Transactions.Add(new Transaction
        {
            UserId = user.Id,
            Type = TransactionType.Renewal,
            Amount = plan.Price,
            BalanceBefore = balanceBefore,
            BalanceAfter = user.Balance,
            PaymentMethod = PaymentMethod.Balance,
            Status = TransactionStatus.Completed,
            RelatedLicenseId = license.Id,
        });

        _context.LicenseEvents.Add(new LicenseEvent
        {
            UserLicenseId = license.Id,
            EventType = "renewed",
            Details = System.Text.Json.JsonSerializer.Serialize(new
            {
                newExpiresAt = license.ExpiresAt,
                durationDays = plan.DurationDays,
            }),
        });

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
            ActivatedAt = license.ActivatedAt,
            ExpiresAt = license.ExpiresAt,
            CurrentActivations = license.CurrentActivations,
            MaxActivations = plan.MaxActivations,
            CreatedAt = license.CreatedAt,
        });
    }
}
