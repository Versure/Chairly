using Chairly.Api.Shared.Tenancy;

namespace Chairly.Tests;

internal sealed class TestTenantContext : ITenantContext
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    public Guid TenantId { get; set; } = DefaultTenantId;
    public Guid UserId { get; set; } = DefaultUserId;
    public string UserRole { get; set; } = "owner";

    public static TestTenantContext Create() => new();
}
