using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.NoShowBooking;

internal sealed record NoShowBookingCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
