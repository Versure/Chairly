# ADR-005: CQRS with Custom Mediator and Vertical Slice Architecture

## Status

Accepted

## Context

We want to follow the CQRS (Command Query Responsibility Segregation) pattern to separate read and write operations. The popular MediatR library implements this pattern but is moving to a commercial license, making it unsuitable for this project.

Additionally, we want to organize backend code by **feature** rather than by **technical layer**, following the Vertical Slice Architecture (VSA) pattern. This keeps all code for a single use case together, making it easier for both humans and AI agents to understand and modify features in isolation.

## Decision

### Vertical Slice Architecture

We use a **hybrid approach**: thin shared layers for domain and infrastructure, with feature slices in the API project.

**Project structure:**

```
src/backend/
├── Chairly.Domain/              # Entities, value objects, shared domain logic
├── Chairly.Infrastructure/      # DbContext, EF configurations, RabbitMQ, Keycloak
├── Chairly.Api/                 # Vertical slices per feature
│   ├── Features/
│   │   ├── Bookings/
│   │   │   ├── CreateBooking/
│   │   │   │   ├── CreateBookingCommand.cs
│   │   │   │   ├── CreateBookingHandler.cs
│   │   │   │   ├── CreateBookingEndpoint.cs
│   │   │   │   └── CreateBookingValidator.cs
│   │   │   ├── GetBooking/
│   │   │   │   ├── GetBookingQuery.cs
│   │   │   │   ├── GetBookingHandler.cs
│   │   │   │   └── GetBookingEndpoint.cs
│   │   │   └── GetBookingsList/
│   │   ├── Clients/
│   │   ├── Staff/
│   │   ├── Services/
│   │   ├── Billing/
│   │   └── Notifications/
│   └── Shared/                  # API-level shared code (middleware, filters)
├── Chairly.AppHost/             # .NET Aspire host
├── Chairly.ServiceDefaults/     # Shared Aspire configuration
└── Chairly.Tests/               # Unit + integration tests
```

**Rules:**
- Each slice (e.g. `CreateBooking/`) contains **everything** for that use case: command/query, handler, validator, endpoint
- Slices in the same bounded context (e.g. `Bookings/`) may reference each other's types
- Slices across bounded contexts should **not** reference each other directly — use domain events or shared contracts
- `Chairly.Domain` contains only entities, value objects, and interfaces — no use-case logic
- `Chairly.Infrastructure` contains only persistence, messaging, and external service integrations

### Custom Mediator

We implement a **custom lightweight mediator** following the MediatR pattern, without depending on the MediatR NuGet package.

The custom implementation includes:
- `IRequest<TResponse>` — marker interface for commands and queries
- `IRequestHandler<TRequest, TResponse>` — handler interface
- `IMediator` — dispatches requests to their handlers
- `IPipelineBehavior<TRequest, TResponse>` — cross-cutting concerns (validation, logging)
- Registration via `IServiceCollection` extension methods using assembly scanning

The mediator lives in `Chairly.Api/Shared/Mediator/` (or a thin shared project if needed by tests).

### Naming Conventions

- Commands: `Create{Entity}Command`, `Update{Entity}Command`, `Delete{Entity}Command`
- Queries: `Get{Entity}Query`, `Get{Entities}ListQuery`
- Handlers: `{CommandOrQuery}Handler`
- Validators: `{CommandOrQuery}Validator`
- Endpoints: `{CommandOrQuery}Endpoint`
- Location: `Api/Features/{Context}/{UseCase}/`

**No event sourcing** — we use simple CQRS without an event store. Domain events are dispatched to RabbitMQ (see ADR-004), not replayed from a store.

## Consequences

- **Positive:** Feature code is co-located — everything for a use case lives in one folder.
- **Positive:** Easy to understand, modify, or delete a feature without touching unrelated code.
- **Positive:** Ideal for AI-driven development — Ralph can implement a full slice in one context window.
- **Positive:** No commercial license dependency — full control over the mediator implementation.
- **Positive:** Pipeline behaviors enable clean cross-cutting concerns (validation, logging, tenant resolution).
- **Negative:** Shared domain entities still live in a separate project — not a pure single-project VSA.
- **Negative:** We own the mediator implementation and must maintain it ourselves.
- **Negative:** Developers must resist the urge to create shared abstractions too early — duplication across slices is acceptable.
