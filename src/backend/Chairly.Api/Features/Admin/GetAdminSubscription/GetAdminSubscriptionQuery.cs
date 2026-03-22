using Chairly.Api.Shared.Mediator;
using OneOf;
using OneOf.Types;

namespace Chairly.Api.Features.Admin.GetAdminSubscription;

internal sealed record GetAdminSubscriptionQuery(Guid Id) : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound>>;
