using System.Security.Claims;
using Chairly.Api.Features.Notifications;
using Chairly.Api.Features.Notifications.GetEmailTemplatesList;
using Chairly.Api.Features.Notifications.PreviewEmailTemplate;
using Chairly.Api.Features.Notifications.UpdateEmailTemplate;
using Chairly.Api.Shared.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;

namespace Chairly.Tests.Features.Notifications;

#pragma warning disable CA1812 // Instantiated via test registration
public class EmailTemplateAuthorizationPolicyTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("manager", true)]
    [InlineData("staff_member", false)]
    [InlineData("unknown", false)]
    [InlineData("", false)]
    public async Task RequireManagerPolicy_EmailTemplates_EnforcesRoleMatrix(string role, bool shouldSucceed)
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
    public async Task EmailTemplateEndpoints_GroupUsesRequireManagerPolicy()
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
        app.MapNotificationEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();
        Assert.NotEmpty(endpoints);

        var emailTemplateEndpoints = endpoints.Where(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            routeEndpoint.RoutePattern.RawText != null &&
            routeEndpoint.RoutePattern.RawText.StartsWith("/api/notifications/email-templates", StringComparison.Ordinal)).ToList();

        Assert.True(emailTemplateEndpoints.Count >= 4, "Expected at least 4 email-template endpoints (GET, PUT, DELETE, POST preview).");

        foreach (var endpoint in emailTemplateEndpoints)
        {
            var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            Assert.Contains(authorizeData, data => string.Equals(data.Policy, "RequireManager", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("POST")]
    public async Task EmailTemplateEndpoints_EachHttpMethodHasRequireManagerPolicy(string httpMethod)
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
        app.MapNotificationEndpoints();

        var endpointRouteBuilder = (IEndpointRouteBuilder)app;
        var endpoints = endpointRouteBuilder.DataSources.SelectMany(ds => ds.Endpoints).ToList();

        var matchingEndpoint = endpoints.FirstOrDefault(endpoint =>
            endpoint is RouteEndpoint routeEndpoint &&
            routeEndpoint.RoutePattern.RawText != null &&
            routeEndpoint.RoutePattern.RawText.StartsWith("/api/notifications/email-templates", StringComparison.Ordinal) &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(httpMethod, StringComparer.Ordinal) == true);

        Assert.NotNull(matchingEndpoint);

        var authorizeData = matchingEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Contains(authorizeData, data => string.Equals(data.Policy, "RequireManager", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true, "owner", true, false)]
    [InlineData(true, "manager", true, false)]
    [InlineData(true, "staff_member", false, true)]
    [InlineData(true, "unknown", false, true)]
    [InlineData(true, "", false, true)]
    [InlineData(false, "manager", false, false)]
    public async Task EmailTemplateAuthorizationStatus_OutcomesMatchAuthAndRole(bool authenticated, string role, bool expectAuthorized, bool expectForbidden)
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
            if (request is GetEmailTemplatesListQuery && typeof(TResponse) == typeof(List<EmailTemplateResponse>))
            {
                var response = new List<EmailTemplateResponse>
                {
                    new(
                        "BookingConfirmation",
                        "Subject",
                        "Main",
                        "Closing",
                        false,
                        ["clientName", "salonName"]),
                };

                return Task.FromResult((TResponse)(object)response);
            }

            if (typeof(TResponse) == typeof(OneOf<EmailTemplateResponse, BadRequest>))
            {
                OneOf<EmailTemplateResponse, BadRequest> response = new EmailTemplateResponse(
                    "BookingConfirmation",
                    "Subject",
                    "Main",
                    "Closing",
                    true,
                    ["clientName", "salonName"]);

                return Task.FromResult((TResponse)(object)response);
            }

            if (typeof(TResponse) == typeof(OneOf<Success, BadRequest>))
            {
                OneOf<Success, BadRequest> response = new Success();

                return Task.FromResult((TResponse)(object)response);
            }

            if (typeof(TResponse) == typeof(OneOf<PreviewEmailTemplateResponse, BadRequest>))
            {
                OneOf<PreviewEmailTemplateResponse, BadRequest> response =
                    new PreviewEmailTemplateResponse("Subject", "<html></html>");

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
