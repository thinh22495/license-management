using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class LicenseProductConfiguration : IEntityTypeConfiguration<LicenseProduct>
{
    public void Configure(EntityTypeBuilder<LicenseProduct> builder)
    {
        builder.ToTable("license_products");
        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.Name).HasMaxLength(255).IsRequired();
        builder.Property(lp => lp.DurationDays).IsRequired();
        builder.Property(lp => lp.MaxActivations).HasDefaultValue(1);
        builder.Property(lp => lp.Price).IsRequired();
        builder.Property(lp => lp.Features).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(lp => lp.IsActive).HasDefaultValue(true);

        builder.HasIndex(lp => lp.ProductId);

        builder.HasOne(lp => lp.Product)
            .WithMany(p => p.LicenseProducts)
            .HasForeignKey(lp => lp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
