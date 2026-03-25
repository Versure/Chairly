using System.Security.Claims;
using Chairly.Api.Shared.Tenancy;

namespace Chairly.Tests.Tenancy;

public class KeycloakRoleClaimTransformerTests
{
    [Fact]
    public async Task TransformAsync_RealmAccessRoles_AddsRoleClaims()
    {
        var transformer = new KeycloakRoleClaimTransformer();
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", """{"roles":["manager","staff_member"]}"""),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);

        Assert.True(transformed.HasClaim(ClaimTypes.Role, "manager"));
        Assert.True(transformed.HasClaim(ClaimTypes.Role, "staff_member"));
    }

    [Fact]
    public async Task TransformAsync_MalformedRealmAccess_DoesNotThrowAndKeepsPrincipal()
    {
        var transformer = new KeycloakRoleClaimTransformer();
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", """{"roles":["manager",}"""),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);

        Assert.Same(principal, transformed);
        Assert.False(transformed.HasClaim(ClaimTypes.Role, "manager"));
    }

    [Fact]
    public async Task TransformAsync_ExistingRole_DoesNotDuplicate()
    {
        var transformer = new KeycloakRoleClaimTransformer();
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", """{"roles":["manager"]}"""),
            new Claim(ClaimTypes.Role, "manager"),
        ], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);
        var roleClaims = transformed.Claims.Where(c => c.Type == ClaimTypes.Role && c.Value == "manager").ToList();

        Assert.Single(roleClaims);
    }

    [Fact]
    public async Task TransformAsync_CustomRoleClaimType_IsInRoleReturnsTrue()
    {
        // JsonWebTokenHandler sets RoleClaimType to "role" (not ClaimTypes.Role).
        // Verify that IsInRole works after transformation in this scenario.
        var transformer = new KeycloakRoleClaimTransformer();
        var identity = new ClaimsIdentity(
        [
            new Claim("realm_access", """{"roles":["manager"]}"""),
        ], "Bearer", nameType: ClaimsIdentity.DefaultNameClaimType, roleType: "role");
        var principal = new ClaimsPrincipal(identity);

        var transformed = await transformer.TransformAsync(principal);

        Assert.True(transformed.IsInRole("manager"));
    }
}
