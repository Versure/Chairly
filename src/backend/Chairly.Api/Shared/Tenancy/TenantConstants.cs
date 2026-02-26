#pragma warning disable MA0026 // Intentional placeholder pending ADR-007 implementation
namespace Chairly.Api.Shared.Tenancy;

internal static class TenantConstants
{
    // TODO: Replace with tenant resolution middleware (ADR-007)
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
#pragma warning restore MA0026
