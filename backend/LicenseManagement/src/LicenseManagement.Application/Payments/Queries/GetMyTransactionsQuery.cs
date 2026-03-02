using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Payments.Queries;

public class GetMyTransactionsQuery : IRequest<PagedResult<TransactionDto>>
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetMyTransactionsQueryHandler : IRequestHandler<GetMyTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly IAppDbContext _context;

    public GetMyTransactionsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<PagedResult<TransactionDto>> Handle(GetMyTransactionsQuery request, CancellationToken ct)
    {
        var query = _context.Transactions
            .Where(t => t.UserId == request.UserId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                PaymentMethod = t.PaymentMethod.HasValue ? t.PaymentMethod.Value.ToString() : null,
                PaymentRef = t.PaymentRef,
                Status = t.Status.ToString(),
                RelatedLicenseId = t.RelatedLicenseId,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync(ct);

        return new PagedResult<TransactionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
