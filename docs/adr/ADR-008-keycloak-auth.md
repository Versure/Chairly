# ADR-008: Keycloak (Self-Hosted) for Authentication

## Status

Accepted

## Context

Chairly needs authentication and authorization for multiple user roles (Owner, Manager, Staff Member) across multiple tenants. We need an identity provider that supports multi-tenancy, role-based access, and standard protocols (OpenID Connect / OAuth 2.0).

## Decision

We use **Keycloak** as a self-hosted identity provider.

### Setup

- Keycloak runs as a Docker container, managed by .NET Aspire in local development (see ADR-002)
- In production, Keycloak is deployed as a separate container alongside the API

### Multi-Tenancy in Keycloak

- Each tenant maps to a **Keycloak realm** (or a single realm with groups — to be finalized during implementation)
- Users are scoped to their tenant's realm
- Tenant context is derived from the JWT token claims

### Integration with .NET

- The API uses ASP.NET Core's built-in OpenID Connect / JWT Bearer authentication
- Keycloak issues JWT tokens; the API validates them against Keycloak's JWKS endpoint
- Role claims (`Owner`, `Manager`, `StaffMember`) are mapped to ASP.NET authorization policies
- A custom `ITenantContext` middleware extracts the tenant identifier from the token

### User Provisioning

- When a new tenant is created, a corresponding realm (or group) is provisioned in Keycloak
- The first user (Owner) is created as part of tenant onboarding
- Staff members are provisioned via the Staff Management feature, which creates both a `StaffMember` entity and a Keycloak user

## Consequences

- **Positive:** Full-featured identity provider out of the box — login UI, password reset, MFA, social login.
- **Positive:** Self-hosted — no vendor lock-in, no per-user costs.
- **Positive:** Standard OpenID Connect — the API has no Keycloak-specific code, just standard JWT validation.
- **Positive:** Keycloak Admin API enables automated tenant/user provisioning.
- **Negative:** Operational overhead — Keycloak must be deployed, monitored, and updated.
- **Negative:** Keycloak is Java-based and relatively resource-heavy for a single-purpose service.
- **Negative:** Realm-per-tenant can become unwieldy at scale (hundreds of tenants); may need to revisit.
