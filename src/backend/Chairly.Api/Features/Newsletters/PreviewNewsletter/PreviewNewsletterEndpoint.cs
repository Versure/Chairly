using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Newsletters.PreviewNewsletter;

internal static class PreviewNewsletterEndpoint
{
    public static void MapPreviewNewsletter(this RouteGroupBuilder group)
    {
        group.MapPost("/preview", async (
            PreviewNewsletterCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
