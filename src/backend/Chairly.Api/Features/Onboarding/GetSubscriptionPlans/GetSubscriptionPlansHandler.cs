using Chairly.Api.Shared.Mediator;

#pragma warning disable CA1812
namespace Chairly.Api.Features.Onboarding.GetSubscriptionPlans;

internal sealed class GetSubscriptionPlansHandler : IRequestHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanResponse>>
{
    private static readonly IReadOnlyList<SubscriptionPlanResponse> _plans =
    [
        new("starter", "Starter", 1, 14.99m, 13.49m),
        new("team", "Team", 5, 59.99m, 53.99m),
        new("salon", "Salon", 15, 149.00m, 134.10m),
    ];

    public Task<IReadOnlyList<SubscriptionPlanResponse>> Handle(GetSubscriptionPlansQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_plans);
    }
}
#pragma warning restore CA1812
