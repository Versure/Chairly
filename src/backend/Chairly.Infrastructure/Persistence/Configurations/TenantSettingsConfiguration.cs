using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.CompanyName)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.CompanyEmail)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.Street)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.HouseNumber)
            .IsRequired(false)
            .HasMaxLength(20);

        builder.Property(x => x.PostalCode)
            .IsRequired(false)
            .HasMaxLength(20);

        builder.Property(x => x.City)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.CompanyPhone)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(x => x.IbanNumber)
            .IsRequired(false)
            .HasMaxLength(34);

        builder.Property(x => x.VatNumber)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired(false);

        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
#pragma warning restore CA1812
