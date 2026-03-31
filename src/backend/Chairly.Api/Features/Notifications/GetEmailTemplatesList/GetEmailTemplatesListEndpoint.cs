using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Notifications.GetEmailTemplatesList;

internal static class GetEmailTemplatesListEndpoint
{
    public static void MapGetEmailTemplatesList(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetEmailTemplatesListQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        });
    }
}
