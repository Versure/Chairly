using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations.Website;

internal sealed class SignUpRequestConfiguration : IEntityTypeConfiguration<SignUpRequest>
{
    public void Configure(EntityTypeBuilder<SignUpRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SignUpRequests");

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

        builder.Property(s => s.CreatedBy)
            .IsRequired(false);

        builder.Property(s => s.ProvisionedBy)
            .IsRequired(false);

        builder.Property(s => s.RejectedBy)
            .IsRequired(false);

        builder.Property(s => s.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.HasIndex(s => s.Email);
        builder.HasIndex(s => s.CreatedAtUtc);
    }
}
#pragma warning restore CA1812
