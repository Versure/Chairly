using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.UpdateStaffMember;

internal static class UpdateStaffMemberEndpoint
{
    public static void MapUpdateStaffMember(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateStaffMemberCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            command.Id = id;
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return result.Match(
                member => Results.Ok(member),
                _ => Results.NotFound());
        });
    }
}
