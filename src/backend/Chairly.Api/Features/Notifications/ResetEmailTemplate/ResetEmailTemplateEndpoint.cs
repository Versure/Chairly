using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.ResetEmailTemplate;

internal static class ResetEmailTemplateEndpoint
{
    public static void MapResetEmailTemplate(this RouteGroupBuilder group)
    {
        group.MapDelete("/{templateType}", async (
            string templateType,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ResetEmailTemplateCommand(templateType), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                _ => Results.BadRequest());
        });
    }
}
