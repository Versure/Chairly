using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.UpdateEmailTemplate;

internal static class UpdateEmailTemplateEndpoint
{
    public static void MapUpdateEmailTemplate(this RouteGroupBuilder group)
    {
        group.MapPut("/{templateType}", async (
            string templateType,
            UpdateEmailTemplateCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.TemplateType = templateType;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.BadRequest());
        });
    }
}
