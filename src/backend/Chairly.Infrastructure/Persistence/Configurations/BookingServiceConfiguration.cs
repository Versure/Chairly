using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("BookingServices");

        builder.HasKey(bs => bs.Id);

        builder.Property(bs => bs.BookingId).IsRequired();
        builder.Property(bs => bs.ServiceId).IsRequired();
        builder.Property(bs => bs.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(bs => bs.Price).HasPrecision(10, 2);
        builder.Property(bs => bs.SortOrder).IsRequired();
    }
}
#pragma warning restore CA1812
