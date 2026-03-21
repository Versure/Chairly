using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Onboarding.GetSubscriptionPlans;

internal static class GetSubscriptionPlansEndpoint
{
    public static void MapGetSubscriptionPlans(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/onboarding/plans", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetSubscriptionPlansQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
