# Phase: Backend Implementation

Detailed instructions for the backend-impl agent. Read the full agent definition
at `.claude/agents/backend-impl.md` for complete patterns.

## Summary

Implement all backend tasks from the spec, working exclusively in `BACKEND_WT`.

## Implementation order

For each backend task (B1 -> B2 -> ...):

1. **Domain entity** (if new) — `{BACKEND_WT}src/backend/Chairly.Domain/Entities/`
2. **EF configuration** (if new entity) — `{BACKEND_WT}src/backend/Chairly.Infrastructure/Persistence/Configurations/`
3. **Migration** (if schema changed) — generate with `dotnet ef migrations add`, then make idempotent
4. **VSA slices** — `{BACKEND_WT}src/backend/Chairly.Api/Features/{Context}/{UseCase}/`
5. **Response record** — shared per context
6. **Endpoint registration** — in `{Context}Endpoints.cs` and `Program.cs`
7. **Unit tests** — `{BACKEND_WT}src/backend/Chairly.Tests/Features/{Context}/`

## Key patterns

- Read `.claude/skills/chairly-backend-slice/SKILL.md` for all boilerplate patterns
- Pragmas: `CA1812` on `internal sealed class`, `MA0026` on `Guid.Empty`
- `.ConfigureAwait(false)` on every `await`
- OneOf for failure cases; direct return for Create and List
- Migrations must be idempotent (raw SQL with IF NOT EXISTS)
- No status columns — timestamp pairs only

## Quality gate

Run build, test, and format after all tasks. Fix failures before reporting.
