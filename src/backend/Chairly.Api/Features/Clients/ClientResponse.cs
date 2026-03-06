namespace Chairly.Api.Features.Clients;

internal sealed record ClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
