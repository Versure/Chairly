using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

internal sealed record GetBookingsListQuery(DateOnly? Date, Guid? StaffMemberId) : IRequest<IReadOnlyList<BookingResponse>>;
