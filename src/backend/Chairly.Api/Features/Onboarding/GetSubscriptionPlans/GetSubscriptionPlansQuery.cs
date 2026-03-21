using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812 // Instantiated via mediator
namespace Chairly.Api.Features.Onboarding.GetSubscriptionPlans;

internal sealed class GetSubscriptionPlansQuery : IRequest<IReadOnlyList<SubscriptionPlanResponse>>
{
}
#pragma warning restore CA1812
