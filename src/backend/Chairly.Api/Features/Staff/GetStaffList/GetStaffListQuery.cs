using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Staff.GetStaffList;

internal sealed record GetStaffListQuery() : IRequest<IEnumerable<StaffMemberResponse>>;
