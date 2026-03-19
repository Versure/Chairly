using System.Security.Claims;
using System.Text.Json;

#pragma warning disable CA1812 // Instantiated via UseMiddleware
namespace Chairly.Api.Shared.Tenancy;

internal sealed partial class TenantContextMiddleware(RequestDelegate next, ILogger<TenantContextMiddleware> logger)
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

        if (!TryPopulateTenantContext(httpContext.User, tenantContext, configuration, out var failureReason))
        {
            LogTenantContextRejected(logger, failureReason);
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            httpContext.Response.Headers.WWWAuthenticate = $"Bearer error=\"invalid_token\", error_description=\"tenant_context_{ToErrorSuffix(failureReason)}\"";
            return;
        }

        await next(httpContext).ConfigureAwait(false);
    }

    internal static bool TryPopulateTenantContext(ClaimsPrincipal user, TenantContext tenantContext, IConfiguration? configuration = null)
    {
        return TryPopulateTenantContext(user, tenantContext, configuration, out _);
    }

    internal static bool TryPopulateTenantContext(
        ClaimsPrincipal user,
        TenantContext tenantContext,
        IConfiguration? configuration,
        out TenantContextFailureReason failureReason)
    {
        if (!TryExtractTenantId(user, configuration, out var tenantId, out failureReason))
        {
            return false;
        }

        tenantContext.TenantId = tenantId;

        var sub = user.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            failureReason = sub is null ? TenantContextFailureReason.MissingSubject : TenantContextFailureReason.InvalidSubject;
            return false;
        }

        tenantContext.UserId = userId;
        if (!TryExtractRole(user, out var role))
        {
            failureReason = TenantContextFailureReason.RoleClaimParsingFailed;
            return false;
        }

        tenantContext.UserRole = role;
        failureReason = TenantContextFailureReason.None;
        return true;
    }

    private static bool TryExtractTenantId(
        ClaimsPrincipal user,
        IConfiguration? configuration,
        out Guid tenantId,
        out TenantContextFailureReason failureReason)
    {
        tenantId = Guid.Empty;
        var issuer = user.FindFirst("iss")?.Value;
        if (issuer is null)
        {
            failureReason = TenantContextFailureReason.MissingIssuer;
            return false;
        }

        var realmsSegment = "/realms/";
        var realmIndex = issuer.LastIndexOf(realmsSegment, StringComparison.Ordinal);
        if (realmIndex < 0)
        {
            failureReason = TenantContextFailureReason.InvalidIssuerFormat;
            return false;
        }

        var realmName = issuer[(realmIndex + realmsSegment.Length)..];

        // Production path: realm name is the tenant GUID itself.
        if (Guid.TryParse(realmName, out tenantId))
        {
            failureReason = TenantContextFailureReason.None;
            return true;
        }

        // Dev path: realm name is human-readable; resolve tenant ID from configuration.
        var configuredTenantId = configuration?["Keycloak:TenantId"];
        var isMapped = configuredTenantId is not null && Guid.TryParse(configuredTenantId, out tenantId);
        failureReason = isMapped ? TenantContextFailureReason.None : TenantContextFailureReason.TenantMappingFailed;
        return isMapped;
    }

    private static bool TryExtractRole(ClaimsPrincipal user, out string role)
    {
        role = string.Empty;

        foreach (var knownRole in _knownRoles)
        {
            if (user.IsInRole(knownRole))
            {
                role = knownRole;
                return true;
            }
        }

        var realmAccessClaim = user.FindFirst("realm_access")?.Value;
        if (realmAccessClaim is null)
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                return true;
            }

            foreach (var roleElement in rolesElement.EnumerateArray())
            {
                var roleValue = roleElement.GetString();
                if (roleValue is not null && Array.Exists(_knownRoles, r => string.Equals(r, roleValue, StringComparison.Ordinal)))
                {
                    role = roleValue;
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // Role claim parsing failures should not force authentication failure.
            // Authorization policies can still evaluate existing role claims and return 403 when needed.
            return true;
        }

        return true;
    }

    private static string ToErrorSuffix(TenantContextFailureReason failureReason) =>
        failureReason switch
        {
            TenantContextFailureReason.MissingIssuer => "missing_issuer",
            TenantContextFailureReason.InvalidIssuerFormat => "invalid_issuer",
            TenantContextFailureReason.TenantMappingFailed => "tenant_mapping",
            TenantContextFailureReason.MissingSubject => "missing_subject",
            TenantContextFailureReason.InvalidSubject => "invalid_subject",
            TenantContextFailureReason.RoleClaimParsingFailed => "claim_parsing",
            _ => "unknown",
        };

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Rejected authenticated request due to tenant context validation failure ({FailureReason}).")]
    private static partial void LogTenantContextRejected(ILogger logger, TenantContextFailureReason failureReason);
}
#pragma warning restore CA1812
