using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Color)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.PhotoUrl)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(s => s.ScheduleJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(s => s.CreatedBy)
            .IsRequired();

        builder.Property(s => s.DeactivatedBy)
            .IsRequired(false);

        builder.Property(s => s.UpdatedBy)
            .IsRequired(false);

        builder.Property(s => s.Role)
            .HasConversion<string>();

        builder.HasIndex(s => new { s.FirstName, s.LastName, s.TenantId });
    }
}
#pragma warning restore CA1812
