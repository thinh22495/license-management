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
            .Select(g => new { Date = g.Key, Amount = g.Sum(t => t.Amount) })
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        // Licenses created by day
        var licensesData = await _context.UserLicenses
            .Where(l => l.CreatedAt >= startDate)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(l => l.Date)
            .ToListAsync(ct);

        // Users registered by day
        var usersData = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
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
            .Select(i => startDate.AddDays(i))
            .ToList();

        var revenueMap = revenueData.ToDictionary(r => r.Date, r => r.Amount);
        var licensesMap = licensesData.ToDictionary(l => l.Date, l => l.Count);
        var usersMap = usersData.ToDictionary(u => u.Date, u => u.Count);

        var result = new ChartsDataDto
        {
            Revenue = allDates.Select(d => new RevenueChartItem
            {
                Date = d.ToString("yyyy-MM-dd"),
                Amount = revenueMap.GetValueOrDefault(d, 0),
            }).ToList(),
            Licenses = allDates.Select(d => new LicenseChartItem
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = licensesMap.GetValueOrDefault(d, 0),
            }).ToList(),
            Users = allDates.Select(d => new UserGrowthItem
            {
                Date = d.ToString("yyyy-MM-dd"),
                Count = usersMap.GetValueOrDefault(d, 0),
            }).ToList(),
            ProductRevenue = productRevenue,
        };

        return ApiResponse<ChartsDataDto>.Ok(result);
    }
}
