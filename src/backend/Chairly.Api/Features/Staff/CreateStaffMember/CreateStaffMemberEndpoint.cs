using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;

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
            return result.Match<IResult>(
                response => Results.Created($"/api/staff/{response.Id}", response),
                (KeycloakError error) => Results.Problem(
                    detail: error.Message,
                    statusCode: StatusCodes.Status502BadGateway));
        });
    }
}
