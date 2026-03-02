using AspNetCoreRateLimit;
using Hangfire;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LicenseManagement.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all EF Core / DbContext registrations added by AddInfrastructure
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IAppDbContext>();
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));

            // Explicitly register InMemory options + context (bypasses AddDbContext provider conflicts)
            var dbName = "TestDb_" + Guid.NewGuid().ToString("N");

            services.AddSingleton(sp =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseInMemoryDatabase(dbName);
                return optionsBuilder.Options;
            });

            services.AddScoped<AppDbContext>(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));

            services.AddScoped<IAppDbContext>(sp =>
                sp.GetRequiredService<AppDbContext>());

            // Disable rate limiting for tests
            services.Configure<IpRateLimitOptions>(options =>
            {
                options.GeneralRules.Clear();
                options.EnableEndpointRateLimiting = false;
            });

            // Remove Hangfire Redis storage and use in-memory
            services.RemoveAll(typeof(IGlobalConfiguration));
            services.AddHangfire(config => config.UseInMemoryStorage());
        });

        builder.UseEnvironment("Development");
    }
}
