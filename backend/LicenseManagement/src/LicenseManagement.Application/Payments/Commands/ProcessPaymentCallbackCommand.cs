using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Application.Payments.Commands;

public class ProcessPaymentCallbackCommand : IRequest<ApiResponse>
{
    public PaymentMethod PaymentMethod { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? Signature { get; set; }
}

public class ProcessPaymentCallbackCommandHandler : IRequestHandler<ProcessPaymentCallbackCommand, ApiResponse>
{
    private readonly IAppDbContext _context;
    private readonly IPaymentGatewayResolver _gatewayResolver;
    private readonly ILogger<ProcessPaymentCallbackCommandHandler> _logger;

    public ProcessPaymentCallbackCommandHandler(
        IAppDbContext context,
        IPaymentGatewayResolver gatewayResolver,
        ILogger<ProcessPaymentCallbackCommandHandler> logger)
    {
        _context = context;
        _gatewayResolver = gatewayResolver;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(ProcessPaymentCallbackCommand request, CancellationToken ct)
    {
        var gateway = _gatewayResolver.Resolve(request.PaymentMethod);

        // Verify signature
        if (!gateway.VerifyCallback(request.Payload, request.Signature ?? ""))
        {
            _logger.LogWarning("Invalid {Provider} callback signature", gateway.ProviderName);
            return ApiResponse.Fail("Invalid signature");
        }

        // Parse callback data
        var callbackResult = gateway.ParseCallback(request.Payload);

        if (!Guid.TryParse(callbackResult.OrderId, out var transactionId))
        {
            _logger.LogWarning("Invalid order ID from {Provider}: {OrderId}", gateway.ProviderName, callbackResult.OrderId);
            return ApiResponse.Fail("Invalid order ID");
        }

        var transaction = await _context.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);
            return ApiResponse.Fail("Transaction not found");
        }

        // Prevent duplicate processing
        if (transaction.Status != TransactionStatus.Pending)
        {
            _logger.LogInformation("Transaction {TransactionId} already processed with status {Status}",
                transactionId, transaction.Status);
            return ApiResponse.Ok("Already processed");
        }

        if (callbackResult.Success)
        {
            // Verify amount matches
            if (callbackResult.Amount != transaction.Amount)
            {
                _logger.LogWarning("Amount mismatch for {TransactionId}: expected {Expected}, got {Actual}",
                    transactionId, transaction.Amount, callbackResult.Amount);
                transaction.Status = TransactionStatus.Failed;
                transaction.Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "amount_mismatch",
                    expected = transaction.Amount,
                    received = callbackResult.Amount,
                });
                await _context.SaveChangesAsync(ct);
                return ApiResponse.Fail("Amount mismatch");
            }

            // Credit user balance
            var user = transaction.User;
            transaction.BalanceBefore = user.Balance;
            user.Balance += transaction.Amount;
            transaction.BalanceAfter = user.Balance;
            transaction.Status = TransactionStatus.Completed;
            transaction.PaymentRef = callbackResult.TransactionRef;

            _logger.LogInformation("TopUp completed: User={UserId}, Amount={Amount}, Balance={Balance}",
                user.Id, transaction.Amount, user.Balance);
        }
        else
        {
            transaction.Status = TransactionStatus.Failed;
            transaction.Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = callbackResult.ErrorMessage,
                transactionRef = callbackResult.TransactionRef,
            });

            _logger.LogWarning("Payment failed for {TransactionId}: {Error}",
                transactionId, callbackResult.ErrorMessage);
        }

        await _context.SaveChangesAsync(ct);
        return ApiResponse.Ok();
    }
}
