using System.Security.Claims;
using System.Text.Json;

#pragma warning disable CA1812 // Instantiated via UseMiddleware
namespace Chairly.Api.Shared.Tenancy;

internal sealed class TenantContextMiddleware(RequestDelegate next)
{
    private static readonly string[] _knownRoles = ["owner", "manager", "staff_member"];

    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await next(httpContext).ConfigureAwait(false);
            return;
        }

        var tenantContext = httpContext.RequestServices.GetRequiredService<TenantContext>();
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (!TryPopulateTenantContext(httpContext.User, tenantContext, configuration))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(httpContext).ConfigureAwait(false);
    }

    internal static bool TryPopulateTenantContext(ClaimsPrincipal user, TenantContext tenantContext, IConfiguration? configuration = null)
    {
        if (!TryExtractTenantId(user, configuration, out var tenantId))
        {
            return false;
        }

        tenantContext.TenantId = tenantId;

        var sub = user.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return false;
        }

        tenantContext.UserId = userId;
        tenantContext.UserRole = ExtractRole(user);
        return true;
    }

    private static bool TryExtractTenantId(ClaimsPrincipal user, IConfiguration? configuration, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        var issuer = user.FindFirst("iss")?.Value;
        if (issuer is null)
        {
            return false;
        }

        var realmsSegment = "/realms/";
        var realmIndex = issuer.LastIndexOf(realmsSegment, StringComparison.Ordinal);
        if (realmIndex < 0)
        {
            return false;
        }

        var realmName = issuer[(realmIndex + realmsSegment.Length)..];

        // Production path: realm name is the tenant GUID itself.
        if (Guid.TryParse(realmName, out tenantId))
        {
            return true;
        }

        // Dev path: realm name is human-readable; resolve tenant ID from configuration.
        var configuredTenantId = configuration?["Keycloak:TenantId"];
        return configuredTenantId is not null && Guid.TryParse(configuredTenantId, out tenantId);
    }

    private static string ExtractRole(ClaimsPrincipal user)
    {
        var realmAccessClaim = user.FindFirst("realm_access")?.Value;
        if (realmAccessClaim is null)
        {
            return string.Empty;
        }

        using var doc = JsonDocument.Parse(realmAccessClaim);
        if (!doc.RootElement.TryGetProperty("roles", out var rolesElement)
            || rolesElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var role in rolesElement.EnumerateArray())
        {
            var roleValue = role.GetString();
            if (roleValue is not null && Array.Exists(_knownRoles, r => string.Equals(r, roleValue, StringComparison.Ordinal)))
            {
                return roleValue;
            }
        }

        return string.Empty;
    }
}
#pragma warning restore CA1812
