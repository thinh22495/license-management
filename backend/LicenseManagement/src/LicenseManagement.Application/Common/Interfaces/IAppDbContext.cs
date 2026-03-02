using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<LicenseProduct> LicenseProducts { get; }
    DbSet<UserLicense> UserLicenses { get; }
    DbSet<LicenseActivation> LicenseActivations { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<PushSubscription> PushSubscriptions { get; }
    DbSet<SigningKey> SigningKeys { get; }
    DbSet<LicenseEvent> LicenseEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
