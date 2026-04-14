using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class NewsletterDeliveryConfiguration : IEntityTypeConfiguration<NewsletterDelivery>
{
    public void Configure(EntityTypeBuilder<NewsletterDelivery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NewsletterDeliveries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CampaignId).IsRequired();
        builder.Property(x => x.ClientId).IsRequired();

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.UnsubscribeToken)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.FailureReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy).IsRequired();

        builder.HasIndex(x => x.UnsubscribeToken).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CampaignId });

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
#pragma warning restore CA1812
