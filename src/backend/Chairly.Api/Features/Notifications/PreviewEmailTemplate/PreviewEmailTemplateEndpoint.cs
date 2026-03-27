using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.PreviewEmailTemplate;

internal static class PreviewEmailTemplateEndpoint
{
    public static void MapPreviewEmailTemplate(this RouteGroupBuilder group)
    {
        group.MapPost("/preview", async (
            PreviewEmailTemplateCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.BadRequest());
        });
    }
}
