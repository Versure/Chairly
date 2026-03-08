using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId).IsRequired();
        builder.Property(b => b.ClientId).IsRequired();
        builder.Property(b => b.StaffMemberId).IsRequired();
        builder.Property(b => b.StartTime).IsRequired();
        builder.Property(b => b.EndTime).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.CreatedBy).IsRequired();
        builder.Property(b => b.UpdatedBy).IsRequired(false);
        builder.Property(b => b.ConfirmedBy).IsRequired(false);
        builder.Property(b => b.StartedBy).IsRequired(false);
        builder.Property(b => b.CompletedBy).IsRequired(false);
        builder.Property(b => b.CancelledBy).IsRequired(false);
        builder.Property(b => b.NoShowBy).IsRequired(false);

        builder.HasOne(b => b.Client)
            .WithMany()
            .HasForeignKey(b => b.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.StaffMember)
            .WithMany()
            .HasForeignKey(b => b.StaffMemberId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.BookingServices)
            .WithOne(bs => bs.Booking)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => new { b.TenantId, b.StaffMemberId, b.StartTime });
        builder.HasIndex(b => new { b.TenantId, b.StartTime });
    }
}
#pragma warning restore CA1812
