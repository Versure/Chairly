# ADR-007: Database-per-Tenant Multi-Tenancy

## Status

Accepted

## Context

Chairly is a multi-tenant SaaS platform where each tenant is a salon. We need to ensure data isolation between tenants while keeping the system manageable. Three strategies were considered:

1. **Row-level isolation** (shared database, TenantId column) — simplest, lowest isolation
2. **Schema-per-tenant** (shared database, separate schemas) — moderate isolation
3. **Database-per-tenant** (separate database per tenant) — maximum isolation

## Decision

We use **database-per-tenant** isolation.

### Architecture

- A **catalog database** stores tenant metadata and connection strings:
  - `Tenants` table: `Id`, `Name`, `Slug`, `ConnectionString`, `CreatedAtUtc`, etc.
- Each tenant gets its own PostgreSQL database with identical schema
- Tenant resolution happens via middleware (subdomain, header, or token claim — to be determined during implementation)
- A `ITenantContext` service provides the current tenant's connection string to EF Core's `DbContext`

### Migration Strategy

- EF Core migrations are generated once and applied to all tenant databases
- A migration runner iterates over all tenants in the catalog and applies pending migrations
- New tenant provisioning includes: create database → apply all migrations → seed default data

### Connection Management

- Connection strings are resolved per-request based on the current tenant
- DbContext is scoped per-request and uses the tenant-specific connection string
- Connection pooling is per-database (handled by Npgsql)

## Consequences

- **Positive:** Maximum data isolation — a bug or query in one tenant cannot access another tenant's data.
- **Positive:** Per-tenant backup, restore, and scaling are straightforward.
- **Positive:** Easier compliance with data residency requirements (tenant database can live in a specific region).
- **Positive:** A single tenant can be migrated to dedicated infrastructure if needed.
- **Negative:** More infrastructure complexity — many databases to manage.
- **Negative:** Migrations must be applied to every tenant database (automated via migration runner).
- **Negative:** Cross-tenant queries (e.g. platform-wide analytics) require querying the catalog and aggregating across databases.
- **Negative:** Higher resource usage — each database has its own connection pool overhead.
