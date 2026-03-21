using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Chairly.Infrastructure.Persistence;

#pragma warning disable CA1812 // Instantiated by EF Core tooling at design time
internal sealed class WebsiteDbContextDesignTimeFactory : IDesignTimeDbContextFactory<WebsiteDbContext>
{
    public WebsiteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WebsiteDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=WebsiteDb");
        return new WebsiteDbContext(optionsBuilder.Options);
    }
}
#pragma warning restore CA1812
