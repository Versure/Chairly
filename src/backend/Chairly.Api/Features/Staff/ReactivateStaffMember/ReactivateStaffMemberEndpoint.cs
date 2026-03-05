using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.ReactivateStaffMember;

internal static class ReactivateStaffMemberEndpoint
{
    public static void MapReactivateStaffMember(this RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/reactivate", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ReactivateStaffMemberCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                member => Results.Ok(member),
                _ => Results.NotFound());
        });
    }
}
