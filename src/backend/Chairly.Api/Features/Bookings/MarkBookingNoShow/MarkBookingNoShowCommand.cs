using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Bookings.MarkBookingNoShow;

internal sealed record MarkBookingNoShowCommand(Guid Id) : IRequest<OneOf<Success, NotFound, Conflict>>;
