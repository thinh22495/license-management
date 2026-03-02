using LicenseManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LicenseManagement.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).HasMaxLength(255).IsRequired();
        builder.Property(n => n.Body).HasColumnType("text").IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasFilter("\"IsRead\" = false");

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .IsRequired(false);
    }
}
