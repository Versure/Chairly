namespace Chairly.Api.Features.Onboarding;

internal sealed record SubmitSignUpRequestResponse(
    Guid Id,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    DateTimeOffset CreatedAtUtc);
