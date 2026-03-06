using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired(false)
            .HasMaxLength(256);

        builder.Property(c => c.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(c => c.Notes)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedBy)
            .IsRequired();

        builder.Property(c => c.UpdatedBy)
            .IsRequired(false);

        builder.Property(c => c.DeletedBy)
            .IsRequired(false);

        builder.HasIndex(c => c.TenantId);
    }
}
#pragma warning restore CA1812
