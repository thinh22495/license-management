using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Users.DTOs;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Users.Commands;

public class AdminTopUpBalanceCommand : IRequest<ApiResponse<UserDto>>
{
    public Guid UserId { get; set; }
    public long Amount { get; set; }
    public string? Note { get; set; }
}

public class AdminTopUpBalanceCommandHandler : IRequestHandler<AdminTopUpBalanceCommand, ApiResponse<UserDto>>
{
    private readonly IAppDbContext _context;

    public AdminTopUpBalanceCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserDto>> Handle(AdminTopUpBalanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return ApiResponse<UserDto>.Fail("Số tiền phải lớn hơn 0");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserDto>.Fail("Người dùng không tồn tại");

        var balanceBefore = user.Balance;
        user.Balance += request.Amount;

        var transaction = new Transaction
        {
            UserId = user.Id,
            Type = TransactionType.TopUp,
            Amount = request.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = user.Balance,
            PaymentMethod = PaymentMethod.Balance,
            Status = TransactionStatus.Completed,
            Metadata = string.IsNullOrEmpty(request.Note)
                ? "{}"
                : System.Text.Json.JsonSerializer.Serialize(new { note = request.Note, source = "admin" })
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Balance = user.Balance,
            IsLocked = user.IsLocked,
            EmailVerified = user.EmailVerified,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}
