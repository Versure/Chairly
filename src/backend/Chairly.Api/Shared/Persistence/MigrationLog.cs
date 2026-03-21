namespace Chairly.Api.Shared.Persistence;

internal static partial class MigrationLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Applying database migrations...")]
    internal static partial void ApplyingMigrations(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied successfully.")]
    internal static partial void MigrationsApplied(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying website database migrations...")]
    internal static partial void ApplyingWebsiteMigrations(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Website database migrations applied successfully.")]
    internal static partial void WebsiteMigrationsApplied(ILogger logger);
}
