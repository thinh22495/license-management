using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Notifications.Commands;
using LicenseManagement.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Jobs;

public class LicenseExpiryJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LicenseExpiryJob> _logger;

    public LicenseExpiryJob(IServiceProvider serviceProvider, ILogger<LicenseExpiryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Check licenses expiring in 7 days and send warnings.
    /// Scheduled daily via Hangfire.
    /// </summary>
    public async Task CheckExpiringLicensesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var sevenDaysLater = now.AddDays(7);
        var oneDayLater = now.AddDays(1);

        // Licenses expiring in 7 days (±12 hours window to avoid duplicates on daily run)
        var expiringIn7Days = await context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .Where(ul => ul.Status == LicenseStatus.Active
                && ul.UserId != null
                && ul.ExpiresAt.HasValue
                && ul.ExpiresAt.Value > sevenDaysLater.AddHours(-12)
                && ul.ExpiresAt.Value <= sevenDaysLater.AddHours(12))
            .ToListAsync();

        foreach (var license in expiringIn7Days)
        {
            _logger.LogInformation("License {LicenseKey} expiring in 7 days for user {UserId}",
                license.LicenseKey, license.UserId);

            await mediator.Send(new SendNotificationCommand
            {
                UserId = license.UserId,
                Title = "License sắp hết hạn (7 ngày)",
                Body = $"License {license.LicenseProduct.Product.Name} - {license.LicenseProduct.Name} sẽ hết hạn vào {license.ExpiresAt:dd/MM/yyyy HH:mm}",
                Type = NotificationType.Warning,
                Channels = ["web", "email"],
            });

            await emailService.SendTemplateAsync(license.User!.Email, "license_expiring", new Dictionary<string, string>
            {
                ["UserName"] = license.User!.FullName,
                ["ProductName"] = license.LicenseProduct.Product.Name,
                ["PlanName"] = license.LicenseProduct.Name,
                ["ExpiresAt"] = license.ExpiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
            });
        }

        // Licenses expiring in 1 day
        var expiringIn1Day = await context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .Where(ul => ul.Status == LicenseStatus.Active
                && ul.UserId != null
                && ul.ExpiresAt.HasValue
                && ul.ExpiresAt.Value > oneDayLater.AddHours(-12)
                && ul.ExpiresAt.Value <= oneDayLater.AddHours(12))
            .ToListAsync();

        foreach (var license in expiringIn1Day)
        {
            _logger.LogInformation("License {LicenseKey} expiring in 1 day for user {UserId}",
                license.LicenseKey, license.UserId);

            await mediator.Send(new SendNotificationCommand
            {
                UserId = license.UserId,
                Title = "License hết hạn ngày mai!",
                Body = $"License {license.LicenseProduct.Product.Name} - {license.LicenseProduct.Name} sẽ hết hạn vào {license.ExpiresAt:dd/MM/yyyy HH:mm}. Hãy gia hạn ngay!",
                Type = NotificationType.Error,
                Channels = ["web", "email"],
            });
        }

        // Auto-expire overdue licenses
        var overdue = await context.UserLicenses
            .Where(ul => ul.Status == LicenseStatus.Active
                && ul.ExpiresAt.HasValue
                && ul.ExpiresAt.Value < now)
            .ToListAsync();

        foreach (var license in overdue)
        {
            license.Status = LicenseStatus.Expired;
            _logger.LogInformation("License {LicenseKey} auto-expired", license.LicenseKey);
        }

        if (overdue.Count > 0)
            await context.SaveChangesAsync();

        _logger.LogInformation("License expiry check completed: {SevenDay} warnings (7d), {OneDay} warnings (1d), {Expired} expired",
            expiringIn7Days.Count, expiringIn1Day.Count, overdue.Count);
    }
}
