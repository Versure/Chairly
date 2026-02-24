# ADR-002: .NET Aspire as Orchestrator for Local Development

## Status

Accepted

## Context

The local development environment requires multiple services: the .NET API, PostgreSQL, RabbitMQ, and Keycloak. We need a way to orchestrate these during development without manual Docker Compose management.

## Decision

We use **.NET Aspire** as the local development orchestrator.

- `Chairly.AppHost` is the Aspire host project that wires up all services
- Aspire manages service discovery, health checks, and the developer dashboard
- External dependencies (PostgreSQL, RabbitMQ, Keycloak) run as Aspire-managed containers
- The Aspire dashboard provides observability (logs, traces, metrics) out of the box

## Consequences

- **Positive:** Single `F5` experience — all services start together with service discovery handled automatically.
- **Positive:** Built-in dashboard for logs, traces, and metrics without extra tooling.
- **Positive:** Aspire generates deployment manifests that can be used for production container orchestration.
- **Negative:** Aspire is relatively new and evolving — API surface may change.
- **Negative:** Frontend (Angular) is not natively managed by Aspire; it runs separately via `nx serve`.
