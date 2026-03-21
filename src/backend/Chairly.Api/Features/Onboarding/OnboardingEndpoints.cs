using Chairly.Api.Features.Onboarding.CreateSubscription;
using Chairly.Api.Features.Onboarding.GetSubscriptionPlans;

namespace Chairly.Api.Features.Onboarding;

internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCreateSubscription();
        app.MapGetSubscriptionPlans();
        return app;
    }
}
