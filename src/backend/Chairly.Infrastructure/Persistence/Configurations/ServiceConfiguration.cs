using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.Price)
            .HasPrecision(10, 2);

        builder.Property(s => s.VatRate)
            .HasPrecision(5, 2)
            .IsRequired(false);

        builder.Property(s => s.CreatedBy)
            .IsRequired();

        builder.Property(s => s.UpdatedBy)
            .IsRequired(false);

        builder.HasIndex(s => new { s.Name, s.TenantId })
            .IsUnique();

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
#pragma warning restore CA1812
