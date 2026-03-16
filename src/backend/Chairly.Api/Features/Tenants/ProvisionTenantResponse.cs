namespace Chairly.Api.Features.Tenants;

internal sealed record ProvisionTenantResponse(
    Guid TenantId,
    string OwnerKeycloakUserId,
    string LoginUrl);
