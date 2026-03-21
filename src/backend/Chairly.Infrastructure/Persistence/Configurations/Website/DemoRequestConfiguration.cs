using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations.Website;

internal sealed class DemoRequestConfiguration : IEntityTypeConfiguration<DemoRequest>
{
    public void Configure(EntityTypeBuilder<DemoRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("DemoRequests");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ContactName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.SalonName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(d => d.Message)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(d => d.CreatedBy)
            .IsRequired(false);

        builder.Property(d => d.ReviewedBy)
            .IsRequired(false);

        builder.HasIndex(d => d.CreatedAtUtc);
    }
}
#pragma warning restore CA1812
