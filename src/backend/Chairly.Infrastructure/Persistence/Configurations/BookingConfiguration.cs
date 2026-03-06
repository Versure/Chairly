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

        ConfigureRequiredProperties(builder);
        ConfigureOptionalProperties(builder);
        ConfigureIndexes(builder);
        ConfigureBookingServices(builder);
    }

    private static void ConfigureRequiredProperties(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.TenantId).IsRequired();
        builder.Property(b => b.ClientId).IsRequired();
        builder.Property(b => b.StaffMemberId).IsRequired();
        builder.Property(b => b.StartTime).IsRequired();
        builder.Property(b => b.EndTime).IsRequired();
        builder.Property(b => b.CreatedBy).IsRequired();
        builder.Property(b => b.Notes).IsRequired(false).HasMaxLength(1000);
    }

    private static void ConfigureOptionalProperties(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.UpdatedAtUtc).IsRequired(false);
        builder.Property(b => b.UpdatedBy).IsRequired(false);
        builder.Property(b => b.ConfirmedAtUtc).IsRequired(false);
        builder.Property(b => b.ConfirmedBy).IsRequired(false);
        builder.Property(b => b.StartedAtUtc).IsRequired(false);
        builder.Property(b => b.StartedBy).IsRequired(false);
        builder.Property(b => b.CompletedAtUtc).IsRequired(false);
        builder.Property(b => b.CompletedBy).IsRequired(false);
        builder.Property(b => b.CancelledAtUtc).IsRequired(false);
        builder.Property(b => b.CancelledBy).IsRequired(false);
        builder.Property(b => b.NoShowAtUtc).IsRequired(false);
        builder.Property(b => b.NoShowBy).IsRequired(false);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Booking> builder)
    {
        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => new { b.TenantId, b.StaffMemberId });
        builder.HasIndex(b => new { b.TenantId, b.StartTime });
    }

    private static void ConfigureBookingServices(EntityTypeBuilder<Booking> builder)
    {
        builder.OwnsMany(b => b.BookingServices, bs =>
        {
            bs.ToTable("BookingServices");
            bs.WithOwner();
            bs.HasKey(s => s.Id);
            bs.Property(s => s.ServiceId).IsRequired();
            bs.Property(s => s.ServiceName).IsRequired().HasMaxLength(200);
            bs.Property(s => s.Price).HasColumnType("decimal(10,2)");
            bs.Property(s => s.SortOrder).IsRequired();
        });
    }
}
#pragma warning restore CA1812
