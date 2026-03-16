namespace Chairly.Api.Shared.Tenancy;

internal interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserRole { get; }
}
