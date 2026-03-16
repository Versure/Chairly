using System.Security.Claims;
using System.Text.Json;
using Chairly.Api.Shared.Tenancy;

namespace Chairly.Tests.Shared;

public class TenantContextMiddlewareTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly string[] OwnerRoles = ["owner"];
    private static readonly string[] ManagerRoles = ["manager", "some_other_role"];
    private static readonly string[] StaffMemberRoles = ["staff_member"];

    private static ClaimsPrincipal CreateAuthenticatedUser(string? issuer = null, string? sub = null, string[]? roles = null)
    {
        var claims = new List<Claim>();

        if (issuer is not null)
        {
            claims.Add(new Claim("iss", issuer));
        }

        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }

        if (roles is not null)
        {
            var realmAccess = JsonSerializer.Serialize(new { roles });
            claims.Add(new Claim("realm_access", realmAccess));
        }

        var identity = new ClaimsIdentity(claims, "Bearer");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void TryPopulateTenantContext_ValidClaims_ParsesTenantIdUserIdAndRole()
    {
        var user = CreateAuthenticatedUser(
            issuer: $"http://localhost:8080/realms/{TestTenantId}",
            sub: TestUserId.ToString(),
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.True(result);
        Assert.Equal(TestTenantId, tenantContext.TenantId);
        Assert.Equal(TestUserId, tenantContext.UserId);
        Assert.Equal("owner", tenantContext.UserRole);
    }

    [Fact]
    public void TryPopulateTenantContext_ManagerRole_ParsesCorrectly()
    {
        var user = CreateAuthenticatedUser(
            issuer: $"http://localhost:8080/realms/{TestTenantId}",
            sub: TestUserId.ToString(),
            roles: ManagerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.True(result);
        Assert.Equal("manager", tenantContext.UserRole);
    }

    [Fact]
    public void TryPopulateTenantContext_StaffMemberRole_ParsesCorrectly()
    {
        var user = CreateAuthenticatedUser(
            issuer: $"http://localhost:8080/realms/{TestTenantId}",
            sub: TestUserId.ToString(),
            roles: StaffMemberRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.True(result);
        Assert.Equal("staff_member", tenantContext.UserRole);
    }

    [Fact]
    public void TryPopulateTenantContext_MalformedIssuer_ReturnsFalse()
    {
        var user = CreateAuthenticatedUser(
            issuer: "http://localhost:8080/not-realms/something",
            sub: TestUserId.ToString(),
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }

    [Fact]
    public void TryPopulateTenantContext_IssuerWithInvalidGuid_ReturnsFalse()
    {
        var user = CreateAuthenticatedUser(
            issuer: "http://localhost:8080/realms/not-a-guid",
            sub: TestUserId.ToString(),
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }

    [Fact]
    public void TryPopulateTenantContext_MissingIssuer_ReturnsFalse()
    {
        var user = CreateAuthenticatedUser(
            issuer: null,
            sub: TestUserId.ToString(),
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }

    [Fact]
    public void TryPopulateTenantContext_MissingSub_ReturnsFalse()
    {
        var user = CreateAuthenticatedUser(
            issuer: $"http://localhost:8080/realms/{TestTenantId}",
            sub: null,
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }

    [Fact]
    public void TryPopulateTenantContext_InvalidSub_ReturnsFalse()
    {
        var user = CreateAuthenticatedUser(
            issuer: $"http://localhost:8080/realms/{TestTenantId}",
            sub: "not-a-guid",
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }

    [Fact]
    public void TryPopulateTenantContext_UnknownIssuerDomain_ReturnsFalse()
    {
        // Issuer contains /realms/ but realm name is not a valid GUID
        var user = CreateAuthenticatedUser(
            issuer: "http://evil-server.com/realms/not-a-valid-guid",
            sub: TestUserId.ToString(),
            roles: OwnerRoles);

        var tenantContext = new TenantContext();

        var result = TenantContextMiddleware.TryPopulateTenantContext(user, tenantContext);

        Assert.False(result);
    }
}
