namespace Chairly.Api.Shared.Tenancy;

internal enum TenantContextFailureReason
{
    None = 0,
    MissingIssuer,
    InvalidIssuerFormat,
    TenantMappingFailed,
    MissingSubject,
    InvalidSubject,
    RoleClaimParsingFailed,
}
