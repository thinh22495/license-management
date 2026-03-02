using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Payments.Gateways;
using LicenseManagement.Infrastructure.Data;
using LicenseManagement.Infrastructure.Jobs;
using LicenseManagement.Infrastructure.Services;
using LicenseManagement.Infrastructure.Services.Email;
using LicenseManagement.Infrastructure.Services.Notifications;
using LicenseManagement.Infrastructure.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LicenseManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            ));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ILicenseCryptoService, LicenseCryptoService>();

        // Payment gateways
        services.AddHttpClient<MoMoGateway>();
        services.AddHttpClient<ZaloPayGateway>();
        services.AddScoped<IPaymentGateway, MoMoGateway>();
        services.AddScoped<IPaymentGateway, VnPayGateway>();
        services.AddScoped<IPaymentGateway, ZaloPayGateway>();
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

        // Notifications
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<ISseNotificationService, SseNotificationService>();

        // Background jobs
        services.AddScoped<LicenseExpiryJob>();

        return services;
    }
}
