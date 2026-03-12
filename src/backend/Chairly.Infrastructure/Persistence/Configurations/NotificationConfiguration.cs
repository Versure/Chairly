using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TenantId).IsRequired();
        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.RecipientType).IsRequired();
        builder.Property(n => n.Channel).IsRequired();
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.ReferenceId).IsRequired();
        builder.Property(n => n.ScheduledAtUtc).IsRequired();
        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.CreatedBy).IsRequired();
        builder.Property(n => n.UpdatedBy).IsRequired(false);
        builder.Property(n => n.SentAtUtc).IsRequired(false);
        builder.Property(n => n.FailedAtUtc).IsRequired(false);
        builder.Property(n => n.FailureReason).HasMaxLength(1000).IsRequired(false);
        builder.Property(n => n.RetryCount).HasDefaultValue(0);

        builder.HasIndex(n => new { n.TenantId, n.ScheduledAtUtc });
        builder.HasIndex(n => new { n.TenantId, n.ReferenceId });
        builder.HasIndex(n => new { n.TenantId, n.CreatedAtUtc }).IsDescending(false, true);
    }
}
#pragma warning restore CA1812
