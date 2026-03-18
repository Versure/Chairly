# Chairly — Copilot Instructions

## Project

Chairly is a multi-tenant SaaS platform for salons and barbershops.

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, OneOf (result pattern)
- **Frontend:** Angular 21, Nx monorepo, NgRx SignalStore, Tailwind CSS v4, SCSS, Sheriff
- **Testing:** xUnit (backend), Vitest (frontend unit), Playwright (frontend e2e)
- **Infra:** PostgreSQL (database-per-tenant), RabbitMQ, Keycloak, .NET Aspire, Docker

## Architecture Overview

- **Backend:** Vertical Slice Architecture (VSA) with thin shared layers. See `docs/adr/` for architecture decisions.
- **Frontend:** Nx monorepo with DDD layers enforced by Sheriff. See `.github/instructions/frontend.instructions.md` for details.
- **Domain model:** See `docs/domain-model.md` for entities, relationships, and ubiquitous language.

## Ubiquitous Language

- **Booking** (never "appointment") — a scheduled visit with one or more services
- **Client** (never "customer") — a person who receives services
- **Staff Member** (never "employee") — a person who works at the salon
- **Service** — a catalog offering (e.g. "Men's Haircut")
- **Tenant** — a single salon location

See `docs/domain-model.md` for the complete glossary.

## User Roles

- **Owner** — full admin, one per tenant
- **Manager** — manages staff and schedules, no billing/settings access
- **Staff Member** — sees and manages own schedule only

## Commit Conventions

Use conventional commits: `feat(bookings): add create booking endpoint`

Prefixes: `feat`, `fix`, `chore`, `refactor`, `test`, `docs`

## Implementation Order

1. Domain entities and value objects
2. EF Core configuration and migration
3. Handlers with validation
4. API endpoints
5. Angular feature (service -> store -> components -> routes)
6. Tests at every step

## Forbidden

- No `any` types in TypeScript
- No `console` statements in production code
- No business logic in controllers/endpoints
- No direct use of DbContext outside Infrastructure layer, except in API slice handlers
- No hardcoded strings for configuration
- No status enum columns — use timestamp pairs (ADR-009)
- No cross-domain imports in the frontend without going through `shared/`
- No MediatR NuGet package — use the custom mediator
- No `@Input()`/`@Output()`/`@ViewChild()` decorators — use signal-based APIs
- No `Subject` + `ngOnDestroy` — use `takeUntilDestroyed(destroyRef)`
- No function calls in Angular templates — use signals or pipes
- No inline styles or inline `template:` in Angular components
- No English user-facing text in the UI — all UI copy must be in Dutch (Nederlands)
- No raw ID inputs in user-facing forms — use searchable dropdowns or selection lists
- Never commit without tests passing
