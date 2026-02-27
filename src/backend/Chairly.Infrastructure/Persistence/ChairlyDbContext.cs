using Chairly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Infrastructure.Persistence;

public class ChairlyDbContext(DbContextOptions<ChairlyDbContext> options) : DbContext(options)
{
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChairlyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
