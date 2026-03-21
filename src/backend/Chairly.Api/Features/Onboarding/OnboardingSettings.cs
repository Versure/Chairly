#pragma warning disable CA1812 // Instantiated via DI (IOptions<OnboardingSettings>)
namespace Chairly.Api.Features.Onboarding;

internal sealed class OnboardingSettings
{
    public string AdminEmail { get; set; } = string.Empty;
}
#pragma warning restore CA1812
