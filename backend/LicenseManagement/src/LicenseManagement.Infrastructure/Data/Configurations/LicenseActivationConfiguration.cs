using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("license_activations");
        builder.HasKey(la => la.Id);

        builder.Property(la => la.HardwareId).HasMaxLength(512).IsRequired();
        builder.Property(la => la.MachineName).HasMaxLength(255);
        builder.Property(la => la.IpAddress).HasMaxLength(45); // IPv6 max length
        builder.Property(la => la.IsActive).HasDefaultValue(true);

        builder.HasIndex(la => la.UserLicenseId);
        builder.HasIndex(la => new { la.UserLicenseId, la.HardwareId }).IsUnique();

        builder.HasOne(la => la.UserLicense)
            .WithMany(ul => ul.Activations)
            .HasForeignKey(la => la.UserLicenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
