using Microsoft.EntityFrameworkCore;

namespace Chairly.Infrastructure.Persistence;

public class ChairlyDbContext(DbContextOptions<ChairlyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChairlyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
