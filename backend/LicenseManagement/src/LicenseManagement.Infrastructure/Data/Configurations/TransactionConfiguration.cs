using LicenseManagement.Domain.Entities;
using LicenseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Amount).IsRequired();
        builder.Property(t => t.BalanceBefore).IsRequired();
        builder.Property(t => t.BalanceAfter).IsRequired();
        builder.Property(t => t.PaymentMethod).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.PaymentRef).HasMaxLength(255);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(TransactionStatus.Pending);
        builder.Property(t => t.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.PaymentRef);
        builder.HasIndex(t => t.Status).HasFilter("\"Status\" = 'Pending'");

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId);

        builder.HasOne(t => t.RelatedLicense)
            .WithMany()
            .HasForeignKey(t => t.RelatedLicenseId)
            .IsRequired(false);
    }
}
