using Chairly.Api.Shared.Mediator;
using Chairly.Api.Shared.Results;
using OneOf;
using OneOf.Types;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Admin.ProvisionSubscription;

internal sealed class ProvisionSubscriptionCommand : IRequest<OneOf<AdminSubscriptionDetailResponse, NotFound, Unprocessable>>
{
    public Guid Id { get; set; }
}
#pragma warning restore CA1812
