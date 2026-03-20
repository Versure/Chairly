# Phase: Infrastructure Review

Detailed instructions for the infra-reviewer agent. Read the full agent definition
at `.claude/agents/infra-reviewer.md` for the complete review checklist.

## Summary

Review all infrastructure implementation against the spec and Chairly conventions.

## Key review areas

1. **Spec compliance** — all infra tasks implemented, configuration values match
2. **Aspire orchestration** — containers, volumes, references, parameters
3. **Service registration** — DI lifetimes, order, background services, async patterns
4. **Keycloak** — multi-issuer JWT, claims transformation, policies, dev seeder
5. **RabbitMQ** — exchange/queue topology, idempotent declarations, manual ACK
6. **Email/SMTP** — Dutch templates, configurable settings, MailDev for dev
7. **Seeding** — dev-only, retry logic, idempotent, API-based
8. **Secrets** — no hardcoded credentials, Aspire parameters used
9. **Health checks** — new services have health checks
10. **Testing** — infrastructure services testable via interfaces

## Output

Return `INFRA-REVIEW-RESULT` block with `status: pass` or `status: issues-found` and
specific findings with file paths.
