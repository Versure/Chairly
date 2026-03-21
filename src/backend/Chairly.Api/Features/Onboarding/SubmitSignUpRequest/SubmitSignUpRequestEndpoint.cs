using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

internal static class SubmitSignUpRequestEndpoint
{
    public static void MapSubmitSignUpRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/onboarding/sign-up-requests", async (
            SubmitSignUpRequestCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/onboarding/sign-up-requests/{result.Id}", result);
        }).AllowAnonymous();
    }
}
