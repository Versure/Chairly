# PRD: Service Catalog — Backend

## Introduction

Implement the Service Catalog backend for Chairly. This is the first real feature, so it includes foundational infrastructure (custom mediator, EF Core + PostgreSQL, DbContext) alongside the Service and ServiceCategory CRUD endpoints.

Services represent the offerings a salon provides (e.g. "Men's Haircut", "Full Color"). ServiceCategories group services (e.g. "Haircuts", "Coloring"). Both are tenant-scoped.

Since authentication is not yet implemented, there are no authorization checks. A hardcoded default TenantId is used for tenant scoping.

## Goals

- Establish the foundational backend infrastructure (mediator, DbContext, EF Core, PostgreSQL)
- Implement full CRUD for ServiceCategory (create, read, update, delete)
- Implement full CRUD for Service (create, read single, read list, update, delete)
- Validate all inputs using Data Annotations
- Follow Vertical Slice Architecture with the custom mediator pattern
- Ensure all code passes `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`

## User Stories

### US-001: Implement custom mediator

**Description:** As a developer, I need the custom mediator infrastructure so that all feature slices can use the CQRS pattern.

**Acceptance Criteria:**
- [ ] `IRequest<TResponse>` marker interface exists in `Chairly.Api/Shared/Mediator/`
- [ ] `IRequestHandler<TRequest, TResponse>` interface exists
- [ ] `IMediator` interface and `Mediator` implementation exist — dispatches requests to handlers
- [ ] `IPipelineBehavior<TRequest, TResponse>` interface exists for cross-cutting concerns
- [ ] `ValidationBehavior<TRequest, TResponse>` pipeline behavior validates requests using Data Annotations before the handler runs. If validation fails, return a `ValidationProblemDetails`-style error (do not throw exceptions)
- [ ] `AddMediator(this IServiceCollection)` extension method registers all handlers and pipeline behaviors via assembly scanning
- [ ] Mediator is registered in `Program.cs`
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-002: Set up EF Core with PostgreSQL and hardcoded tenant

**Description:** As a developer, I need the database infrastructure so that entities can be persisted to PostgreSQL.

**Acceptance Criteria:**
- [ ] `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.EntityFrameworkCore` packages added to `Chairly.Infrastructure`
- [ ] `Microsoft.EntityFrameworkCore.Design` package added to `Chairly.Api` (for migrations CLI)
- [ ] `ChairlyDbContext` created in `Chairly.Infrastructure/Persistence/` — inherits `DbContext`
- [ ] DbContext is registered in `Program.cs` with the PostgreSQL connection string from `appsettings.Development.json`
- [ ] A hardcoded default `TenantId` constant exists (e.g. in `Chairly.Api/Shared/Tenancy/TenantConstants.cs`) for use until real tenant resolution is implemented — add a `// TODO: Replace with tenant resolution middleware (ADR-007)` comment
- [ ] Connection string added to `appsettings.Development.json` under `ConnectionStrings:ChairlyDb` pointing to `Host=localhost;Database=chairly_dev;Username=postgres;Password=postgres`
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-003: Create ServiceCategory and Service domain entities

**Description:** As a developer, I need the domain entities so that the service catalog data can be modeled.

**Acceptance Criteria:**
- [ ] `ServiceCategory` entity in `Chairly.Domain/Entities/` with properties: `Id` (Guid), `TenantId` (Guid), `Name` (string), `SortOrder` (int)
- [ ] `Service` entity in `Chairly.Domain/Entities/` with properties: `Id` (Guid), `TenantId` (Guid), `Name` (string), `Description` (string, nullable), `Duration` (TimeSpan), `Price` (decimal), `CategoryId` (Guid, nullable), `IsActive` (bool), `SortOrder` (int), `CreatedAtUtc` (DateTimeOffset), `UpdatedAtUtc` (DateTimeOffset, nullable)
- [ ] `Service` has a navigation property to `ServiceCategory` (optional relationship)
- [ ] No EF Core attributes on domain entities — configuration is in Infrastructure
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-004: Add EF Core configurations and initial migration

**Description:** As a developer, I need EF Core entity configurations and an initial migration so that the database schema is created.

**Acceptance Criteria:**
- [ ] `ServiceCategoryConfiguration` in `Chairly.Infrastructure/Persistence/Configurations/` implementing `IEntityTypeConfiguration<ServiceCategory>`
- [ ] `ServiceConfiguration` in `Chairly.Infrastructure/Persistence/Configurations/` implementing `IEntityTypeConfiguration<Service>`
- [ ] Service.Name has a max length and is required
- [ ] Service.Name + TenantId has a unique index (service name unique within tenant)
- [ ] ServiceCategory.Name has a max length and is required
- [ ] Service.Price is configured with `precision(10, 2)`
- [ ] Service.CategoryId is an optional FK to ServiceCategory with `SetNull` on delete
- [ ] `DbSet<Service>` and `DbSet<ServiceCategory>` added to `ChairlyDbContext`
- [ ] Initial EF Core migration created (named `InitialCreate` or `AddServiceCatalog`)
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-005: Create ServiceCategory CRUD endpoints

**Description:** As an API consumer, I want to manage service categories so that services can be organized into groups.

**Acceptance Criteria:**
- [ ] `CreateServiceCategory/` slice: command with `Name` (required) and `SortOrder` (int), handler creates entity with hardcoded TenantId, endpoint `POST /api/service-categories` returns 201 with created entity
- [ ] `GetServiceCategoriesList/` slice: query returns all categories for the hardcoded tenant ordered by SortOrder, endpoint `GET /api/service-categories` returns 200
- [ ] `UpdateServiceCategory/` slice: command with `Id`, `Name`, `SortOrder`, handler updates entity, endpoint `PUT /api/service-categories/{id}` returns 200 with updated entity, 404 if not found
- [ ] `DeleteServiceCategory/` slice: command with `Id`, handler deletes entity, endpoint `DELETE /api/service-categories/{id}` returns 204, 404 if not found
- [ ] All commands have Data Annotations validation (e.g. `[Required]` on Name, `[MaxLength]`)
- [ ] Endpoints are mapped in a `ServiceCategoryEndpoints` extension method and registered in `Program.cs`
- [ ] Handlers use `OneOf` for result pattern — e.g. `OneOf<ServiceCategoryResponse, NotFound>` for update/delete
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-006: Create Service CRUD endpoints

**Description:** As an API consumer, I want to manage services so that the salon's offerings are maintained in the catalog.

**Acceptance Criteria:**
- [ ] `CreateService/` slice: command with `Name` (required), `Description` (optional), `Duration` (required, TimeSpan), `Price` (required, decimal >= 0), `CategoryId` (optional Guid), `SortOrder` (int), handler creates entity with `IsActive = true` and hardcoded TenantId, endpoint `POST /api/services` returns 201
- [ ] `GetService/` slice: query by Id, endpoint `GET /api/services/{id}` returns 200 with service details (including category name if set), 404 if not found
- [ ] `GetServicesList/` slice: query returns all services for the hardcoded tenant ordered by SortOrder, includes category name, endpoint `GET /api/services` returns 200
- [ ] `UpdateService/` slice: command with all editable fields, handler updates entity and sets `UpdatedAtUtc`, endpoint `PUT /api/services/{id}` returns 200, 404 if not found
- [ ] `DeleteService/` slice: command with `Id`, handler deletes entity, endpoint `DELETE /api/services/{id}` returns 204, 404 if not found
- [ ] All commands have Data Annotations validation
- [ ] Handlers use `OneOf` for result pattern
- [ ] Endpoints are mapped in a `ServiceEndpoints` extension method and registered in `Program.cs`
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-007: Toggle Service active status

**Description:** As an API consumer, I want to activate or deactivate a service so that it can be hidden from the catalog without deleting it.

**Acceptance Criteria:**
- [ ] `ToggleServiceActive/` slice: command with `Id`, handler toggles `IsActive` and sets `UpdatedAtUtc`, endpoint `PATCH /api/services/{id}/toggle-active` returns 200 with updated service, 404 if not found
- [ ] Handler uses `OneOf` for result pattern
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

### US-008: Add unit tests for Service and ServiceCategory handlers

**Description:** As a developer, I need unit tests to verify business logic in the handlers.

**Acceptance Criteria:**
- [ ] Unit tests for `CreateServiceCategoryHandler` — happy path + validation failure
- [ ] Unit tests for `UpdateServiceCategoryHandler` — happy path + not found
- [ ] Unit tests for `DeleteServiceCategoryHandler` — happy path + not found
- [ ] Unit tests for `CreateServiceHandler` — happy path + validation failure
- [ ] Unit tests for `UpdateServiceHandler` — happy path + not found
- [ ] Unit tests for `DeleteServiceHandler` — happy path + not found
- [ ] Unit tests for `ToggleServiceActiveHandler` — toggles true→false and false→true + not found
- [ ] Tests use an in-memory database or SQLite provider for the DbContext
- [ ] `dotnet test src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes

## Functional Requirements

- FR-1: The custom mediator dispatches requests to the correct handler based on the request type
- FR-2: The validation pipeline behavior rejects invalid requests before the handler executes, returning structured validation errors
- FR-3: ServiceCategory supports CRUD operations: create, list, update, delete
- FR-4: Service supports CRUD operations: create, get by id, list, update, delete, toggle active
- FR-5: Service.Name must be unique within a tenant — duplicate names return a validation error
- FR-6: All entities are scoped to a hardcoded TenantId until tenant resolution middleware is implemented
- FR-7: Deleting a ServiceCategory sets `CategoryId` to null on related services (EF Core `SetNull` behavior)
- FR-8: Service list and detail responses include the category name when a category is assigned
- FR-9: All API responses use consistent DTOs (never expose domain entities directly)

## Non-Goals

- No authentication or authorization (Keycloak not yet integrated)
- No real multi-tenancy middleware (hardcoded TenantId for now)
- No frontend implementation (backend only)
- No .NET Aspire AppHost wiring for PostgreSQL (developer runs PostgreSQL locally or via Docker)
- No pagination, filtering, or search on list endpoints (keep it simple)
- No soft-delete — services are deactivated via `IsActive`, categories are hard-deleted

## Technical Considerations

- Follow ADR-005 for slice structure: `Chairly.Api/Features/Services/{UseCase}/`
- Follow ADR-003 for EF Core: separate `IEntityTypeConfiguration<T>` classes in Infrastructure
- Follow ADR-009 for timestamps: `CreatedAtUtc` and `UpdatedAtUtc` on Service (ServiceCategory is simpler — no timestamps needed beyond what EF tracks)
- Use `OneOf` for result pattern in handlers (e.g. `OneOf<ServiceResponse, NotFound, ValidationFailed>`)
- Use Data Annotations on command/query classes for input validation
- All endpoints via Minimal APIs, grouped per feature context
- The solution must pass: `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`

## Success Metrics

- All 8 user stories implemented and passing
- `dotnet build src/backend/Chairly.slnx` — zero errors
- `dotnet test src/backend/Chairly.slnx` — all tests green
- `dotnet format src/backend/Chairly.slnx --verify-no-changes` — no formatting issues
- Ralph creates a PR with all changes

## Open Questions

- None — all decisions are captured in ADRs and this PRD.
