using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(255).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Description).HasColumnType("text");
        builder.Property(p => p.IconUrl).HasMaxLength(500);
        builder.Property(p => p.WebsiteUrl).HasMaxLength(500);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
    }
}
