using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#pragma warning disable CA1812 // Instantiated via ApplyConfigurationsFromAssembly
namespace Chairly.Infrastructure.Persistence.Configurations;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("EmailTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.TemplateType).IsRequired();

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedBy).IsRequired(false);

        builder.HasIndex(x => new { x.TenantId, x.TemplateType }).IsUnique();
    }
}
#pragma warning restore CA1812
