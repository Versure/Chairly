using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Infrastructure.Persistence;

public class WebsiteDbContext(DbContextOptions<WebsiteDbContext> options) : DbContext(options)
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WebsiteDbContext).Assembly,
            type => type.Namespace?.Contains("Website", StringComparison.Ordinal) == true);
        base.OnModelCreating(modelBuilder);
    }
}
