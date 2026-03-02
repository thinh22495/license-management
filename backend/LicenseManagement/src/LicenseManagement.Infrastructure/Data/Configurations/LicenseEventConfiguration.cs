using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class LicenseEventConfiguration : IEntityTypeConfiguration<LicenseEvent>
{
    public void Configure(EntityTypeBuilder<LicenseEvent> builder)
    {
        builder.ToTable("license_events");
        builder.HasKey(le => le.Id);

        builder.Property(le => le.EventType).HasMaxLength(50).IsRequired();
        builder.Property(le => le.Details).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(le => le.IpAddress).HasMaxLength(45);

        builder.HasIndex(le => le.UserLicenseId);

        builder.HasOne(le => le.UserLicense)
            .WithMany(ul => ul.Events)
            .HasForeignKey(le => le.UserLicenseId);
    }
}
