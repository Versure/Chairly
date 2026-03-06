using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.CancelBooking;

internal sealed record CancelBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
