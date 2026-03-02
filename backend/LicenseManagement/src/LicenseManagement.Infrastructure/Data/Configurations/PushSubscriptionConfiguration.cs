using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("push_subscriptions");
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Endpoint).HasColumnType("text").IsRequired();
        builder.Property(ps => ps.P256dhKey).HasColumnType("text").IsRequired();
        builder.Property(ps => ps.AuthKey).HasColumnType("text").IsRequired();

        builder.HasIndex(ps => new { ps.UserId, ps.Endpoint }).IsUnique();

        builder.HasOne(ps => ps.User)
            .WithMany(u => u.PushSubscriptions)
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
