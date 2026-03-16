#pragma warning disable CA1812 // Instantiated via DI
namespace Chairly.Api.Shared.Tenancy;

internal sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}
#pragma warning restore CA1812
