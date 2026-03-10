using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.GetBooking;

internal sealed record GetBookingQuery(Guid Id) : IRequest<OneOf<BookingResponse, NotFound>>;
