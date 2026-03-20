# Phase: Infrastructure Implementation

Detailed instructions for the infra-impl agent. Read the full agent definition
at `.claude/agents/infra-impl.md` for complete patterns.

## Summary

Implement all infrastructure tasks from the spec, working exclusively in `BACKEND_WT`.
Infra tasks cover Aspire orchestration, Keycloak, RabbitMQ, SMTP, seeding, and
cross-cutting service registration.

## Key areas

1. **Aspire orchestration** — `Chairly.AppHost/Program.cs`, container definitions, resource wiring
2. **Keycloak** — JWT validation, claims transformation, dev seeder, authorization policies
3. **RabbitMQ** — exchange/queue topology, publishers, consumers
4. **Email/SMTP** — templates (Dutch), dispatcher, MailDev for dev
5. **Seeding** — dev-only, retry logic, idempotent, via API not EF HasData
6. **Service registration** — `Program.cs` DI, middleware pipeline, health checks
7. **Configuration** — `appsettings.json`, Aspire parameters, no hardcoded secrets

## Key patterns

- Read `.claude/agents/infra-impl.md` for full implementation guidelines per area
- `.ConfigureAwait(false)` on every `await`
- Async patterns in Program.cs follow CA2007/MA0004/CA1849 rules
- Aspire parameters for credentials (never hardcode secrets)
- Dev seeders guard with environment check + retry logic

## Quality gate

Run build, test, and format after all tasks. Fix failures before reporting.
