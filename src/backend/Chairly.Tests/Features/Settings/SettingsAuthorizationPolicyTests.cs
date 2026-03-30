using System.Security.Claims;
using Chairly.Api.Features.Settings;
using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OneOf.Types;

namespace Chairly.Tests.Features.Settings;

#pragma warning disable CA1812 // Instantiated via test registration
public class SettingsAuthorizationPolicyTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("manager", true)]
    [InlineData("staff_member", false)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public async Task RequireManagerPolicy_SettingsWrite_EnforcesRoleMatrix(string role, bool shouldSucceed)
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
    public async Task SettingsEndpoints_VatWriteGroupUsesRequireManagerPolicy()
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
        app.MapSettingsEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();

        var updateVatEndpoint = endpoints.Single(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            string.Equals(routeEndpoint.RoutePattern.RawText, "/api/settings/vat", StringComparison.Ordinal) &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("PUT", StringComparer.Ordinal) == true);

        var authorizeData = updateVatEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(authorizeData, data => string.Equals(data.Policy, "RequireManager", StringComparison.Ordinal));
        Assert.DoesNotContain(authorizeData, data => string.Equals(data.Policy, "RequireOwner", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SettingsEndpoints_CompanyWriteGroupUsesRequireManagerPolicy()
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
        app.MapSettingsEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();

        var updateCompanyEndpoint = endpoints.Single(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            string.Equals(routeEndpoint.RoutePattern.RawText, "/api/settings/company/", StringComparison.Ordinal) &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("PUT", StringComparer.Ordinal) == true);

        var authorizeData = updateCompanyEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(authorizeData, data => string.Equals(data.Policy, "RequireManager", StringComparison.Ordinal));
        Assert.DoesNotContain(authorizeData, data => string.Equals(data.Policy, "RequireOwner", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SettingsEndpoints_ReadGroupsStillUseRequireStaffPolicy()
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
        app.MapSettingsEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();

        var getVatEndpoint = endpoints.Single(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            string.Equals(routeEndpoint.RoutePattern.RawText, "/api/settings/vat", StringComparison.Ordinal) &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET", StringComparer.Ordinal) == true);

        var vatAuthorizeData = getVatEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(vatAuthorizeData, data => string.Equals(data.Policy, "RequireStaff", StringComparison.Ordinal));

        var getCompanyEndpoint = endpoints.Single(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            string.Equals(routeEndpoint.RoutePattern.RawText, "/api/settings/company/", StringComparison.Ordinal) &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET", StringComparer.Ordinal) == true);

        var companyAuthorizeData = getCompanyEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(companyAuthorizeData, data => string.Equals(data.Policy, "RequireStaff", StringComparison.Ordinal));
    }

    private sealed class StubMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (typeof(TResponse) == typeof(VatSettingsResponse))
            {
                return Task.FromResult((TResponse)(object)new VatSettingsResponse(21m));
            }

            if (typeof(TResponse) == typeof(CompanyInfoResponse))
            {
                return Task.FromResult((TResponse)(object)new CompanyInfoResponse(
                    null, null, null, null, null, null, null, null, null, null));
            }

            if (typeof(TResponse) == typeof(OneOf.OneOf<CompanyInfoResponse, NotFound>))
            {
                var response = new CompanyInfoResponse(
                    null, null, null, null, null, null, null, null, null, null);
                OneOf.OneOf<CompanyInfoResponse, NotFound> oneOf = response;
                return Task.FromResult((TResponse)(object)oneOf);
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
