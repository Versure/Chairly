---
applyTo: "src/backend/**"
---

# Backend Conventions — Vertical Slice Architecture

## Projects

- `Chairly.Domain` — Entities, value objects, enums. No use-case logic. No EF Core dependency.
- `Chairly.Infrastructure` — DbContext, EF configurations, RabbitMQ, Keycloak integration.
- `Chairly.Api` — Vertical slices organized by feature. Contains the custom mediator.
- `Chairly.AppHost` — .NET Aspire orchestrator for local development.
- `Chairly.ServiceDefaults` — Shared Aspire configuration.
- `Chairly.Tests` — Unit and integration tests.

## Slice Structure

```
Chairly.Api/Features/{Context}/{UseCase}/
  ├── {UseCase}Command.cs or {UseCase}Query.cs
  ├── {UseCase}Handler.cs
  ├── {UseCase}Endpoint.cs
  └── {UseCase}Validator.cs
```

## Rules

- Each slice contains everything for one use case: command/query, handler, validator, endpoint
- Slices in the same bounded context may reference each other
- Slices across bounded contexts must NOT reference each other — use domain events or shared contracts
- Business logic lives in handlers, NEVER in endpoints

## Custom Mediator (ADR-005)

Custom mediator (no MediatR package). Located in `Chairly.Api/Shared/Mediator/`.
Interfaces: `IRequest<TResponse>`, `IRequestHandler<TRequest, TResponse>`, `IMediator`, `IPipelineBehavior<TRequest, TResponse>`

## Naming

- Commands: `Create{Entity}Command`, `Update{Entity}Command`, `Delete{Entity}Command`
- Queries: `Get{Entity}Query`, `Get{Entities}ListQuery`
- Handlers: `{CommandOrQuery}Handler`
- Validators: `{CommandOrQuery}Validator`
- Endpoints: `{CommandOrQuery}Endpoint`

## Patterns

- Use OneOf for the result pattern (no exceptions for business logic)
- Data Annotations + manual validation for input validation
- Entity configurations in separate `IEntityTypeConfiguration<T>` classes
- All endpoints via Minimal APIs, grouped per feature in extension methods
- Timestamps instead of status columns (ADR-009): `{Action}AtUtc` + `{Action}By` pairs
- `CreatedAtUtc`/`CreatedBy` required on all entities
- Database-per-tenant: all entities carry `TenantId`, tenant resolution via middleware
- EF Core migrations must be idempotent (use `IF NOT EXISTS` patterns)
- Append `.ConfigureAwait(false)` to every `await` expression

## Async patterns in Program.cs

- Avoid `await using` — use try/finally with `await scope.DisposeAsync().ConfigureAwait(false)` (CA2007/MA0004)
- Use `await app.RunAsync().ConfigureAwait(false)` instead of `app.Run()` (CA1849)

## Quality Checks

```bash
dotnet build src/backend/Chairly.slnx
dotnet test src/backend/Chairly.slnx
dotnet format src/backend/Chairly.slnx --verify-no-changes
```

If `dotnet format` fails, auto-fix with `dotnet format src/backend/Chairly.slnx` then verify again.
