using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.BookingId).IsRequired();
        builder.Property(i => i.ClientId).IsRequired();

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.InvoiceDate).IsRequired();

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.CreatedBy).IsRequired();
        builder.Property(i => i.SentBy).IsRequired(false);
        builder.Property(i => i.PaidBy).IsRequired(false);
        builder.Property(i => i.VoidedBy).IsRequired(false);

        builder.OwnsMany(i => i.LineItems, lineItem =>
        {
            lineItem.ToTable("InvoiceLineItems");

            lineItem.WithOwner().HasForeignKey("InvoiceId");
            lineItem.HasKey(li => li.Id);

            lineItem.Property(li => li.Description)
                .IsRequired()
                .HasMaxLength(200);

            lineItem.Property(li => li.Quantity).IsRequired();

            lineItem.Property(li => li.UnitPrice)
                .HasPrecision(18, 2);

            lineItem.Property(li => li.TotalPrice)
                .HasPrecision(18, 2);

            lineItem.Property(li => li.SortOrder).IsRequired();
        });

        builder.HasIndex(i => new { i.TenantId, i.BookingId }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.TenantId, i.ClientId });
        builder.HasIndex(i => new { i.TenantId, i.CreatedAtUtc })
            .IsDescending(false, true);
    }
}
#pragma warning restore CA1812
