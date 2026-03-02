namespace LicenseManagement.Application.Dashboard.DTOs;

public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveLicenses { get; set; }
    public int TotalProducts { get; set; }
    public long RevenueThisMonth { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int LicensesPurchasedThisMonth { get; set; }
}

public class RevenueChartItem
{
    public string Date { get; set; } = string.Empty;
    public long Amount { get; set; }
}

public class LicenseChartItem
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserGrowthItem
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ProductRevenueItem
{
    public string ProductName { get; set; } = string.Empty;
    public long Revenue { get; set; }
    public int LicenseCount { get; set; }
}
