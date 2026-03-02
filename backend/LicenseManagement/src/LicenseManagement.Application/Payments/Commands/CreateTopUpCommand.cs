using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Payments.DTOs;
using LicenseManagement.Application.Payments.Gateways;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Payments.Commands;

public class CreateTopUpCommand : IRequest<ApiResponse<TopUpResultDto>>
{
    public Guid UserId { get; set; }
    public long Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class CreateTopUpCommandHandler : IRequestHandler<CreateTopUpCommand, ApiResponse<TopUpResultDto>>
{
    private readonly IAppDbContext _context;
    private readonly IPaymentGatewayResolver _gatewayResolver;

    public CreateTopUpCommandHandler(IAppDbContext context, IPaymentGatewayResolver gatewayResolver)
    {
        _context = context;
        _gatewayResolver = gatewayResolver;
    }

    public async Task<ApiResponse<TopUpResultDto>> Handle(CreateTopUpCommand request, CancellationToken ct)
    {
        if (request.Amount < 10_000)
            return ApiResponse<TopUpResultDto>.Fail("Số tiền nạp tối thiểu là 10,000 VND");

        if (request.Amount > 50_000_000)
            return ApiResponse<TopUpResultDto>.Fail("Số tiền nạp tối đa là 50,000,000 VND");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user == null)
            return ApiResponse<TopUpResultDto>.Fail("Người dùng không tồn tại");

        if (user.IsLocked)
            return ApiResponse<TopUpResultDto>.Fail("Tài khoản đã bị khóa");

        // Create pending transaction
        var transaction = new Transaction
        {
            UserId = request.UserId,
            Type = TransactionType.TopUp,
            Amount = request.Amount,
            BalanceBefore = user.Balance,
            BalanceAfter = user.Balance, // Will be updated on callback
            PaymentMethod = request.PaymentMethod,
            Status = TransactionStatus.Pending,
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(ct);

        // Create payment order via gateway
        var gateway = _gatewayResolver.Resolve(request.PaymentMethod);
        var orderResult = await gateway.CreateOrderAsync(new CreateOrderParams
        {
            OrderId = transaction.Id.ToString(),
            Amount = request.Amount,
            Description = $"Nạp {request.Amount:N0} VND vào tài khoản License Management",
            ReturnUrl = request.ReturnUrl,
            NotifyUrl = request.NotifyUrl,
            IpAddress = request.IpAddress,
        });

        if (!orderResult.Success)
        {
            transaction.Status = TransactionStatus.Failed;
            transaction.Metadata = System.Text.Json.JsonSerializer.Serialize(new { error = orderResult.ErrorMessage });
            await _context.SaveChangesAsync(ct);
            return ApiResponse<TopUpResultDto>.Fail($"Tạo đơn thanh toán thất bại: {orderResult.ErrorMessage}");
        }

        transaction.PaymentRef = orderResult.TransactionRef;
        await _context.SaveChangesAsync(ct);

        return ApiResponse<TopUpResultDto>.Ok(new TopUpResultDto
        {
            TransactionId = transaction.Id,
            PaymentUrl = orderResult.PaymentUrl,
            Status = "pending",
        });
    }
}
