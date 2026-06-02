using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class NewsletterCampaignConfiguration : IEntityTypeConfiguration<NewsletterCampaign>
{
    public void Configure(EntityTypeBuilder<NewsletterCampaign> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NewsletterCampaigns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BodyHtml)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.RecipientFilter)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ScheduledAtUtc).IsRequired(false);
        builder.Property(x => x.ScheduledBy).IsRequired(false);
        builder.Property(x => x.QueuedAtUtc).IsRequired(false);
        builder.Property(x => x.QueuedBy).IsRequired(false);
        builder.Property(x => x.SentAtUtc).IsRequired(false);
        builder.Property(x => x.SentBy).IsRequired(false);
        builder.Property(x => x.CancelledAtUtc).IsRequired(false);
        builder.Property(x => x.CancelledBy).IsRequired(false);

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired(false);

        builder.HasIndex(x => new { x.TenantId, x.ScheduledAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });

        builder.HasMany(x => x.Deliveries)
            .WithOne()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
#pragma warning restore CA1812
