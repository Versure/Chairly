using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.DeactivateStaffMember;

internal static class DeactivateStaffMemberEndpoint
{
    public static void MapDeactivateStaffMember(this RouteGroupBuilder group)
    {
        group.MapPatch("/{id:guid}/deactivate", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeactivateStaffMemberCommand(id), cancellationToken).ConfigureAwait(false);
            return result.Match(
                member => Results.Ok(member),
                _ => Results.NotFound());
        });
    }
}
