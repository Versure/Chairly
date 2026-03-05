using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Staff.ReactivateStaffMember;

internal sealed record ReactivateStaffMemberCommand(Guid Id) : IRequest<OneOf<StaffMemberResponse, NotFound>>;
