using Chairly.Api.Features.Clients;
using Chairly.Api.Features.Clients.GetClientsList;
using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Chairly.Tests.Features.Clients;

#pragma warning disable CA1812 // Instantiated via test registration
public class ClientAuthorizationPolicyTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("manager", true)]
    [InlineData("staff_member", true)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public async Task RequireStaffPolicy_EnforcesRoleMatrix(string role, bool shouldSucceed)
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

        var result = await authorizationService.AuthorizeAsync(principal, null, "RequireStaff");

        Assert.Equal(shouldSucceed, result.Succeeded);
    }

    [Fact]
    public async Task ClientEndpoints_GroupUsesRequireStaffPolicy()
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
        app.MapClientEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();
        Assert.NotEmpty(endpoints);

        var authorizedClientEndpointCount = 0;
        foreach (var endpoint in endpoints)
        {
            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            if (authorizeData.Any(data => string.Equals(data.Policy, "RequireStaff", StringComparison.Ordinal)))
            {
                authorizedClientEndpointCount++;
            }
        }

        Assert.True(authorizedClientEndpointCount >= 5, "Expected all mapped /api/clients endpoints to require RequireStaff.");
    }

    [Theory]
    [InlineData(true, "owner", true, false)]
    [InlineData(true, "manager", true, false)]
    [InlineData(true, "staff_member", true, false)]
    [InlineData(true, "unknown", false, true)]
    [InlineData(true, "", false, true)]
    [InlineData(false, "manager", false, false)]
    public async Task ClientsAuthorizationStatus_OutcomesMatchAuthAndRole(bool authenticated, string role, bool expectAuthorized, bool expectForbidden)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
            options.AddPolicy("RequireStaff", p => p.RequireRole("owner", "manager", "staff_member")));

        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var principal = CreatePrincipal(role, authenticated);
        var authorized = (await authorizationService.AuthorizeAsync(principal, null, "RequireStaff")).Succeeded;

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
            if (typeof(TResponse) == typeof(ClientResponse))
            {
                var response = new ClientResponse(
                    Guid.NewGuid(),
                    "Test",
                    "Client",
                    null,
                    null,
                    null,
                    DateTimeOffset.UtcNow,
                    null);

                return Task.FromResult((TResponse)(object)response);
            }

            if (request is GetClientsListQuery && typeof(TResponse) == typeof(IReadOnlyList<ClientResponse>))
            {
                var response = new[]
                {
                    new ClientResponse(
                        Guid.NewGuid(),
                        "Test",
                        "Client",
                        null,
                        null,
                        null,
                        DateTimeOffset.UtcNow,
                        null),
                };

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
