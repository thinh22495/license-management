using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LicenseManagement.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded");
            return;
        }

        logger.LogInformation("Seeding database...");

        // Admin user
        var admin = new User
        {
            Email = "thinh95.tranhuu@gmail.com",
            Phone = "0900000000",
            FullName = "System Admin",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            Balance = 0,
            EmailVerified = true,
        };

        // Demo user
        var demoUser = new User
        {
            Email = "user@demo.com",
            Phone = "0911111111",
            FullName = "Demo User",
            PasswordHash = passwordHasher.HashPassword("User@123"),
            Role = UserRole.User,
            Balance = 5_000_000,
            EmailVerified = true,
        };

        context.Users.AddRange(admin, demoUser);

        // Products
        var product1 = new Product
        {
            Name = "ScreenCapture Pro",
            Slug = "screencapture-pro",
            Description = "Phần mềm chụp và quay màn hình chuyên nghiệp, hỗ trợ nhiều định dạng và chỉnh sửa trực tiếp.",
            IsActive = true,
            Metadata = "{}",
        };

        var product2 = new Product
        {
            Name = "CodeEditor Ultimate",
            Slug = "codeeditor-ultimate",
            Description = "Trình soạn thảo code đa ngôn ngữ với AI autocomplete, debugging tích hợp và extensions phong phú.",
            IsActive = true,
            Metadata = "{}",
        };

        var product3 = new Product
        {
            Name = "DataSync Manager",
            Slug = "datasync-manager",
            Description = "Đồng bộ dữ liệu đa nền tảng, backup tự động và khôi phục nhanh chóng.",
            IsActive = true,
            Metadata = "{}",
        };

        context.Products.AddRange(product1, product2, product3);

        // License plans for ScreenCapture Pro
        context.LicenseProducts.AddRange(
            new LicenseProduct
            {
                Product = product1,
                Name = "Basic - 1 tháng",
                DurationDays = 30,
                MaxActivations = 1,
                Price = 99_000,
                Features = "[\"capture\",\"basic_edit\"]",
                IsActive = true,
            },
            new LicenseProduct
            {
                Product = product1,
                Name = "Pro - 1 năm",
                DurationDays = 365,
                MaxActivations = 3,
                Price = 799_000,
                Features = "[\"capture\",\"record\",\"advanced_edit\",\"cloud_storage\"]",
                IsActive = true,
            },
            new LicenseProduct
            {
                Product = product1,
                Name = "Lifetime",
                DurationDays = 0,
                MaxActivations = 5,
                Price = 1_999_000,
                Features = "[\"capture\",\"record\",\"advanced_edit\",\"cloud_storage\",\"priority_support\"]",
                IsActive = true,
            }
        );

        // License plans for CodeEditor Ultimate
        context.LicenseProducts.AddRange(
            new LicenseProduct
            {
                Product = product2,
                Name = "Personal - 1 tháng",
                DurationDays = 30,
                MaxActivations = 1,
                Price = 149_000,
                Features = "[\"syntax_highlight\",\"autocomplete\"]",
                IsActive = true,
            },
            new LicenseProduct
            {
                Product = product2,
                Name = "Team - 1 năm",
                DurationDays = 365,
                MaxActivations = 5,
                Price = 1_499_000,
                Features = "[\"syntax_highlight\",\"autocomplete\",\"ai_complete\",\"debugging\",\"extensions\"]",
                IsActive = true,
            }
        );

        // License plans for DataSync Manager
        context.LicenseProducts.AddRange(
            new LicenseProduct
            {
                Product = product3,
                Name = "Starter - 6 tháng",
                DurationDays = 180,
                MaxActivations = 2,
                Price = 299_000,
                Features = "[\"sync\",\"backup\"]",
                IsActive = true,
            },
            new LicenseProduct
            {
                Product = product3,
                Name = "Enterprise - Vĩnh viễn",
                DurationDays = 0,
                MaxActivations = 10,
                Price = 2_999_000,
                Features = "[\"sync\",\"backup\",\"restore\",\"multi_cloud\",\"api_access\"]",
                IsActive = true,
            }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeded successfully");
    }
}
