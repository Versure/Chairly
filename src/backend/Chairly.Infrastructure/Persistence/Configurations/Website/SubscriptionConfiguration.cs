using Chairly.Domain.Entities;
using Chairly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations.Website;

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SalonName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.OwnerFirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.OwnerLastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(s => s.Plan)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.BillingCycle)
            .HasConversion<string?>()
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(s => s.CreatedBy)
            .IsRequired(false);

        builder.Property(s => s.ProvisionedBy)
            .IsRequired(false);

        builder.Property(s => s.CancelledBy)
            .IsRequired(false);

        builder.Property(s => s.CancellationReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Ignore(s => s.IsTrial);

        builder.HasIndex(s => s.Email);
        builder.HasIndex(s => s.CreatedAtUtc);
        builder.HasIndex(s => s.Plan);
    }
}
#pragma warning restore CA1812
