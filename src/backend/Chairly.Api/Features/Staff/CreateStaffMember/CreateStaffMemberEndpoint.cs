using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.CreateStaffMember;

internal static class CreateStaffMemberEndpoint
{
    public static void MapCreateStaffMember(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateStaffMemberCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/staff/{result.Id}", result);
        });
    }
}
