using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.DeactivateStaffMember;

internal sealed record DeactivateStaffMemberCommand(Guid Id) : IRequest<OneOf<StaffMemberResponse, NotFound>>;
