using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Dashboard.DTOs;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Dashboard.Queries;

public class GetDashboardStatsQuery : IRequest<ApiResponse<DashboardStatsDto>> { }

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, ApiResponse<DashboardStatsDto>>
{
    private readonly IAppDbContext _context;

    public GetDashboardStatsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var stats = new DashboardStatsDto
        {
            TotalUsers = await _context.Users.CountAsync(ct),
            ActiveLicenses = await _context.UserLicenses.CountAsync(l => l.Status == LicenseStatus.Active, ct),
            TotalProducts = await _context.Products.CountAsync(p => p.IsActive, ct),
            RevenueThisMonth = await _context.Transactions
                .Where(t => t.Type == TransactionType.Purchase && t.Status == TransactionStatus.Completed && t.CreatedAt >= startOfMonth)
                .SumAsync(t => t.Amount, ct),
            NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= startOfMonth, ct),
            LicensesPurchasedThisMonth = await _context.UserLicenses.CountAsync(l => l.CreatedAt >= startOfMonth, ct),
        };

        return ApiResponse<DashboardStatsDto>.Ok(stats);
    }
}
