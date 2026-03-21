using Chairly.Api.Features.Onboarding.SubmitDemoRequest;
using Chairly.Api.Features.Onboarding.SubmitSignUpRequest;

namespace Chairly.Api.Features.Onboarding;

internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSubmitDemoRequest();
        app.MapSubmitSignUpRequest();
        return app;
    }
}
