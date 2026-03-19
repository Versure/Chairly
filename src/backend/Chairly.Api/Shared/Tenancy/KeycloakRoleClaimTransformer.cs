using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Shared.Tenancy;

internal sealed class KeycloakRoleClaimTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        var realmAccessClaim = principal.FindFirst("realm_access")?.Value;
        if (realmAccessClaim is null)
        {
            return Task.FromResult(principal);
        }

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (!doc.RootElement.TryGetProperty("roles", out var rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                return Task.FromResult(principal);
            }

            foreach (var role in rolesElement.EnumerateArray())
            {
                var roleValue = role.GetString();
                if (roleValue is not null && !identity.HasClaim(ClaimTypes.Role, roleValue))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                }
            }
        }
        catch (JsonException)
        {
            // Keep original principal; malformed role claims are handled by tenant middleware diagnostics.
        }

        return Task.FromResult(principal);
    }
}
#pragma warning restore CA1812
