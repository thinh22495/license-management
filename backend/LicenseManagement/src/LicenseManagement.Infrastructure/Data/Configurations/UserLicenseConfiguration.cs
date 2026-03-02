using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class UserLicenseConfiguration : IEntityTypeConfiguration<UserLicense>
{
    public void Configure(EntityTypeBuilder<UserLicense> builder)
    {
        builder.ToTable("user_licenses");
        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.LicenseKey).HasMaxLength(512).IsRequired();
        builder.HasIndex(ul => ul.LicenseKey).IsUnique();

        builder.Property(ul => ul.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(LicenseStatus.Active);
        builder.Property(ul => ul.CurrentActivations).HasDefaultValue(0);
        builder.Property(ul => ul.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(ul => ul.UserId);
        builder.HasIndex(ul => ul.ExpiresAt).HasFilter("\"Status\" = 'Active'");

        builder.HasOne(ul => ul.User)
            .WithMany(u => u.Licenses)
            .HasForeignKey(ul => ul.UserId);

        builder.HasOne(ul => ul.LicenseProduct)
            .WithMany(lp => lp.UserLicenses)
            .HasForeignKey(ul => ul.LicenseProductId);
    }
}
