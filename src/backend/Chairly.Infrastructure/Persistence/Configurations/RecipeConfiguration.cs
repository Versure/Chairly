using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.ClientId).IsRequired();
        builder.Property(r => r.StaffMemberId).IsRequired();

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Notes)
            .HasMaxLength(2000);

        builder.Property(r => r.CreatedBy).IsRequired();
        builder.Property(r => r.UpdatedBy).IsRequired(false);

        builder.OwnsMany(r => r.Products, p =>
        {
            p.ToTable("RecipeProducts");
            p.WithOwner().HasForeignKey("RecipeId");
            p.HasKey(x => x.Id);

            p.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            p.Property(x => x.Brand)
                .HasMaxLength(100);

            p.Property(x => x.Quantity)
                .HasMaxLength(50);

            p.Property(x => x.SortOrder).IsRequired();
        });

        builder.HasIndex(r => new { r.TenantId, r.BookingId }).IsUnique();
        builder.HasIndex(r => new { r.TenantId, r.ClientId });
    }
}
#pragma warning restore CA1812
