using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class SigningKeyConfiguration : IEntityTypeConfiguration<SigningKey>
{
    public void Configure(EntityTypeBuilder<SigningKey> builder)
    {
        builder.ToTable("signing_keys");
        builder.HasKey(sk => sk.Id);

        builder.Property(sk => sk.Algorithm).HasMaxLength(20).HasDefaultValue("Ed25519");
        builder.Property(sk => sk.PublicKey).HasColumnType("text").IsRequired();
        builder.Property(sk => sk.PrivateKeyEnc).HasColumnType("text").IsRequired();
        builder.Property(sk => sk.IsActive).HasDefaultValue(true);

        builder.HasIndex(sk => sk.ProductId);

        builder.HasOne(sk => sk.Product)
            .WithMany(p => p.SigningKeys)
            .HasForeignKey(sk => sk.ProductId);
    }
}
