using System.Security.Claims;
using Chairly.Api.Features.Staff;
using Chairly.Api.Features.Staff.GetStaffList;
using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Chairly.Tests.Features.Staff;

#pragma warning disable CA1812 // Instantiated via test registration
public class StaffAuthorizationPolicyTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("manager", true)]
    [InlineData("staff_member", false)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public async Task RequireManagerPolicy_CreateStaff_EnforcesRoleMatrix(string role, bool shouldSucceed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireOwner", p => p.RequireRole("owner"));
            options.AddPolicy("RequireManager", p => p.RequireRole("owner", "manager"));
            options.AddPolicy("RequireStaff", p => p.RequireRole("owner", "manager", "staff_member"));
        });

        using var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(role);

        var result = await authorizationService.AuthorizeAsync(principal, null, "RequireManager");

        Assert.Equal(shouldSucceed, result.Succeeded);
    }

    [Fact]
    public async Task StaffEndpoints_WriteGroupUsesRequireManagerPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireOwner", p => p.RequireRole("owner"));
            options.AddPolicy("RequireManager", p => p.RequireRole("owner", "manager"));
            options.AddPolicy("RequireStaff", p => p.RequireRole("owner", "manager", "staff_member"));
        });
        builder.Services.AddSingleton<IMediator>(new StubMediator());

        await using var app = builder.Build();
        app.MapStaffEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();
        Assert.NotEmpty(endpoints);

        var createEndpoint = endpoints.Single(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            string.Equals(routeEndpoint.RoutePattern.RawText, "/api/staff/", StringComparison.Ordinal) &&
            routeEndpoint.RoutePattern.Parameters.Count == 0 &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST", StringComparer.Ordinal) == true);

        var authorizeData = createEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(authorizeData, data => string.Equals(data.Policy, "RequireManager", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, "owner", true, false)]
    [InlineData(true, "manager", true, false)]
    [InlineData(true, "staff_member", false, true)]
    [InlineData(true, "unknown", false, true)]
    [InlineData(true, "", false, true)]
    [InlineData(false, "manager", false, false)]
    public async Task CreateStaffAuthorizationStatus_OutcomesMatchAuthAndRole(bool authenticated, string role, bool expectAuthorized, bool expectForbidden)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
            options.AddPolicy("RequireManager", p => p.RequireRole("owner", "manager")));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var principal = CreatePrincipal(role, authenticated);
        var authorized = (await authorizationService.AuthorizeAsync(principal, null, "RequireManager")).Succeeded;

        Assert.Equal(expectAuthorized, authorized);

        if (!authenticated)
        {
            Assert.False(expectForbidden);
            return;
        }

        Assert.Equal(expectForbidden, !authorized);
    }

    private sealed class StubMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetStaffListQuery && typeof(TResponse) == typeof(IReadOnlyList<StaffMemberResponse>))
            {
                var response = new[]
                {
                    new StaffMemberResponse(
                        Guid.NewGuid(),
                        "Test",
                        "Staff",
                        "test@chairly.nl",
                        "staff_member",
                        "#000000",
                        null,
                        true,
                        new Dictionary<string, ShiftBlockResponse[]>(StringComparer.OrdinalIgnoreCase),
                        DateTimeOffset.UtcNow,
                        null),
                };

                return Task.FromResult((TResponse)(object)response);
            }

            if (typeof(TResponse) == typeof(StaffMemberResponse))
            {
                var response = new StaffMemberResponse(
                    Guid.NewGuid(),
                    "Test",
                    "Staff",
                    "test@chairly.nl",
                    "staff_member",
                    "#000000",
                    null,
                    true,
                    new Dictionary<string, ShiftBlockResponse[]>(StringComparer.OrdinalIgnoreCase),
                    DateTimeOffset.UtcNow,
                    null);

                return Task.FromResult((TResponse)(object)response);
            }

            throw new InvalidOperationException($"No stub response configured for {typeof(TResponse).Name}");
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string role, bool authenticated = true)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = authenticated
            ? new ClaimsIdentity(claims, "Bearer")
            : new ClaimsIdentity();

        return new ClaimsPrincipal(identity);
    }
}
#pragma warning restore CA1812
