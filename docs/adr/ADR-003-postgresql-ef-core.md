# ADR-003: PostgreSQL with EF Core (Code-First Migrations)

## Status

Accepted

## Context

We need a relational database for persistent storage and an ORM for data access. The application uses Vertical Slice Architecture (see ADR-005) with a thin `Chairly.Infrastructure` project for persistence concerns.

## Decision

We use **PostgreSQL** as the database and **Entity Framework Core** with **code-first migrations** for data access.

- EF Core is used exclusively in the `Chairly.Infrastructure` project
- Entity configurations use separate `IEntityTypeConfiguration<T>` classes
- Migrations are generated via `dotnet ef migrations add` and applied via `dotnet ef database update`
- The `Chairly.Domain` project has no dependency on EF Core
- Npgsql is the PostgreSQL provider for EF Core

## Consequences

- **Positive:** Code-first migrations keep the schema in sync with the domain model and are version-controlled.
- **Positive:** PostgreSQL is open-source, performant, and well-supported by EF Core.
- **Positive:** Separate `IEntityTypeConfiguration<T>` classes keep entity configuration organized and maintainable.
- **Negative:** EF Core migrations require care in a database-per-tenant setup (see ADR-007) — migrations must be applied to all tenant databases.
- **Negative:** Complex queries may require raw SQL or falling back to Npgsql directly.
