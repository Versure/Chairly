using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

#pragma warning disable CA1812 // Instantiated via reflection by EF Core tooling
namespace Chairly.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChairlyDbContext>
{
    public ChairlyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseNpgsql("Host=localhost;Database=chairly_design;Username=postgres;Password=postgres")
            .Options;
        return new ChairlyDbContext(options);
    }
}
#pragma warning restore CA1812
