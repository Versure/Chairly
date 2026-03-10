using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Clients.GetRecipeByBooking;

internal sealed record GetRecipeByBookingQuery(Guid BookingId) : IRequest<OneOf<RecipeResponse, NotFound, Forbidden>>;
