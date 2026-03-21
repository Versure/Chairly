using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Onboarding.SubmitDemoRequest;

internal static class SubmitDemoRequestEndpoint
{
    public static void MapSubmitDemoRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/onboarding/demo-requests", async (
            SubmitDemoRequestCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Created($"/api/onboarding/demo-requests/{response.Id}", response),
                unprocessable => Results.UnprocessableEntity(new { message = unprocessable.Message }));
        }).AllowAnonymous();
    }
}
