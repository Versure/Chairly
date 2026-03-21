namespace Chairly.Api.Features.Onboarding;

internal sealed record SubmitDemoRequestResponse(
    Guid Id,
    string ContactName,
    string SalonName,
    string Email,
    DateTimeOffset CreatedAtUtc);
