using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Onboarding.CreateSubscription;

internal static class CreateSubscriptionEndpoint
{
    public static void MapCreateSubscription(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/onboarding/subscriptions", async (
            CreateSubscriptionCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Created($"/api/onboarding/subscriptions/{response.Id}", response),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        }).AllowAnonymous();
    }
}
