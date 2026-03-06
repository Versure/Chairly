using Chairly.Api.Shared.Mediator;

namespace Chairly.Api.Features.Bookings.GetBookingsList;

#pragma warning disable CA1812
internal sealed class GetBookingsListQuery : IRequest<IEnumerable<BookingResponse>>
{
    public DateOnly? Date { get; set; }

    public Guid? StaffMemberId { get; set; }
}
#pragma warning restore CA1812
