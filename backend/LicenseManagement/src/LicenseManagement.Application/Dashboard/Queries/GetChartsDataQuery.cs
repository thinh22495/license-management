using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Dashboard.DTOs;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Dashboard.Queries;

public class GetChartsDataQuery : IRequest<ApiResponse<ChartsDataDto>>
{
    public int Days { get; set; } = 30;
}

public class ChartsDataDto
{
    public List<RevenueChartItem> Revenue { get; set; } = [];
    public List<LicenseChartItem> Licenses { get; set; } = [];
    public List<UserGrowthItem> Users { get; set; } = [];
    public List<ProductRevenueItem> ProductRevenue { get; set; } = [];
}

public class GetChartsDataQueryHandler : IRequestHandler<GetChartsDataQuery, ApiResponse<ChartsDataDto>>
{
    private readonly IAppDbContext _context;

    public GetChartsDataQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<ChartsDataDto>> Handle(GetChartsDataQuery request, CancellationToken ct)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-request.Days);

        // Revenue by day (from completed purchase + topup transactions)
        var revenueData = await _context.Transactions
            .Where(t => t.Status == TransactionStatus.Completed
                && t.Type == TransactionType.Purchase
                && t.CreatedAt >= startDate)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new RevenueChartItem
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Amount = g.Sum(t => t.Amount),
            })
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        // Licenses created by day
        var licensesData = await _context.UserLicenses
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new LicenseChartItem
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
            })
            .OrderBy(l => l.Date)
            .ToListAsync(ct);

        // Users registered by day
        var usersData = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new UserGrowthItem
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
            })
            .OrderBy(u => u.Date)
            .ToListAsync(ct);

        // Revenue breakdown by product
        var productRevenue = await _context.UserLicenses
            .Include(l => l.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.LicenseProduct.Product.Name)
            .Select(g => new ProductRevenueItem
            {
                ProductName = g.Key,
                LicenseCount = g.Count(),
                Revenue = g.Sum(l => l.LicenseProduct.Price),
            })
            .OrderByDescending(p => p.Revenue)
            .ToListAsync(ct);

        // Fill missing dates with zeros
        var allDates = Enumerable.Range(0, request.Days + 1)
            .Select(i => startDate.AddDays(i).ToString("yyyy-MM-dd"))
            .ToList();

        var revenueMap = revenueData.ToDictionary(r => r.Date);
        var licensesMap = licensesData.ToDictionary(l => l.Date);
        var usersMap = usersData.ToDictionary(u => u.Date);

        var result = new ChartsDataDto
        {
            Revenue = allDates.Select(d => revenueMap.TryGetValue(d, out var v)
                ? v : new RevenueChartItem { Date = d, Amount = 0 }).ToList(),
            Licenses = allDates.Select(d => licensesMap.TryGetValue(d, out var v)
                ? v : new LicenseChartItem { Date = d, Count = 0 }).ToList(),
            Users = allDates.Select(d => usersMap.TryGetValue(d, out var v)
                ? v : new UserGrowthItem { Date = d, Count = 0 }).ToList(),
            ProductRevenue = productRevenue,
        };

        return ApiResponse<ChartsDataDto>.Ok(result);
    }
}
