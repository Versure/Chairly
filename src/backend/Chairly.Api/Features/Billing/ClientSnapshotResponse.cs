namespace Chairly.Api.Features.Billing;

internal sealed record ClientSnapshotResponse(
    string FullName,
    string? Email,
    string? Phone,
    string? Address);
