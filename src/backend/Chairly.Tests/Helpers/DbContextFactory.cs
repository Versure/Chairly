using Chairly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chairly.Tests.Helpers;

/// <summary>
/// Shared factory for creating in-memory DbContext instances in unit tests.
/// Eliminates the need for each test file to duplicate the same setup code.
/// </summary>
internal static class DbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="ChairlyDbContext"/> backed by a uniquely named in-memory database.
    /// Each call returns an isolated database to prevent test cross-contamination.
    /// </summary>
    public static ChairlyDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ChairlyDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ChairlyDbContext(options);
    }
}
