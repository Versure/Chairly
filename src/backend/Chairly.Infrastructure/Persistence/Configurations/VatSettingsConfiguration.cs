using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class VatSettingsConfiguration : IEntityTypeConfiguration<VatSettings>
{
    public void Configure(EntityTypeBuilder<VatSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).IsRequired();

        builder.Property(v => v.DefaultVatRate)
            .HasPrecision(5, 2);

        builder.Property(v => v.CreatedBy).IsRequired();
        builder.Property(v => v.UpdatedBy).IsRequired(false);

        builder.HasIndex(v => v.TenantId).IsUnique();
    }
}
#pragma warning restore CA1812
