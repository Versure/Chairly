using Chairly.Infrastructure.Keycloak;

namespace Chairly.Api.Shared.Tenancy;

internal static partial class KeycloakDevSeeder
{
    private const string DefaultEmail = "manager@chairly.local";
    private const string DefaultPassword = "Chairly123!";
    private const string DefaultFirstName = "Chairly";
    private const string DefaultLastName = "Manager";
    private const string DefaultRole = "manager";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("KeycloakDevSeeder");
        var keycloak = services.GetRequiredService<IKeycloakAdminService>();

        var realmString = configuration["Keycloak:Realm"]
            ?? throw new InvalidOperationException("Keycloak:Realm configuration is required for dev seeding.");
        var tenantId = Guid.Parse(realmString);

        // Step 1: Create realm (skip if already exists).
        try
        {
            await keycloak.CreateRealmAsync(tenantId, DefaultEmail, ct).ConfigureAwait(false);
            LogRealmCreated(logger, tenantId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            LogRealmAlreadyExists(logger, tenantId);
        }

        // Step 2: Create user (skip if already exists).
        string? userId;
        try
        {
            userId = await keycloak.CreateUserAsync(
                tenantId, DefaultEmail, DefaultFirstName, DefaultLastName, DefaultRole, ct).ConfigureAwait(false);
            LogUserCreated(logger, DefaultEmail);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            LogUserAlreadyExists(logger, DefaultEmail);
            LogSeedComplete(logger, DefaultEmail, DefaultPassword);
            return;
        }

        if (userId is null)
        {
            return;
        }

        // Step 3: Set password.
        await keycloak.SetPasswordAsync(tenantId, userId, DefaultPassword, temporary: false, ct).ConfigureAwait(false);
        LogPasswordSet(logger, DefaultEmail);

        // Step 4: Assign manager role.
        await keycloak.AssignRealmRoleAsync(tenantId, userId, DefaultRole, ct).ConfigureAwait(false);
        LogRoleAssigned(logger, DefaultRole, DefaultEmail);

        LogSeedComplete(logger, DefaultEmail, DefaultPassword);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created realm {TenantId}")]
    private static partial void LogRealmCreated(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: realm {TenantId} already exists, skipping creation")]
    private static partial void LogRealmAlreadyExists(ILogger logger, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: created user {Email}")]
    private static partial void LogUserCreated(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: user {Email} already exists, skipping setup")]
    private static partial void LogUserAlreadyExists(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: password set for {Email}")]
    private static partial void LogPasswordSet(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed: assigned role '{Role}' to {Email}")]
    private static partial void LogRoleAssigned(ILogger logger, string role, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Keycloak dev seed complete. Login: {Email} / {Password}")]
    private static partial void LogSeedComplete(ILogger logger, string email, string password);
}
