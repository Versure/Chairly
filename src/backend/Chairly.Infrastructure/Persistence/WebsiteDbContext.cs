using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Infrastructure.Persistence;

public class WebsiteDbContext(DbContextOptions<WebsiteDbContext> options) : DbContext(options)
{
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<SignUpRequest> SignUpRequests => Set<SignUpRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(WebsiteDbContext).Assembly,
            type => type.Namespace?.Contains("Website", StringComparison.Ordinal) == true);
        base.OnModelCreating(modelBuilder);
    }
}
