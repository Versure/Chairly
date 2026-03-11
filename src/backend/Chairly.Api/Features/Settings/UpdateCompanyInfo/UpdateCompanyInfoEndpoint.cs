using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Settings.UpdateCompanyInfo;

internal static class UpdateCompanyInfoEndpoint
{
    public static void MapUpdateCompanyInfo(this RouteGroupBuilder group)
    {
        group.MapPut("/", async (
            UpdateCompanyInfoCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                response => Results.Ok(response),
                _ => Results.Forbid());
        });
    }
}
