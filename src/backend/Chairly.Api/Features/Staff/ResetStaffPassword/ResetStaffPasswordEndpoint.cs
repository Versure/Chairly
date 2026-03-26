using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.ResetStaffPassword;

internal static class ResetStaffPasswordEndpoint
{
    public static void MapResetStaffPassword(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/reset-password", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ResetStaffPasswordCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                _ => Results.Ok(),
                _ => Results.NotFound());
        });
    }
}
