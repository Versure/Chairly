# Phase: Frontend Implementation

Detailed instructions for the frontend-impl agent. Read the full agent definition
at `.claude/agents/frontend-impl.md` for complete patterns.

## Summary

Implement all frontend tasks from the spec, working exclusively in `FRONTEND_WT`.

## Implementation order

For each frontend task (F1 -> F2 -> ...):

1. **Models** — `{domain}/models/{entity}.models.ts`
2. **API service** — `{domain}/data-access/{entity}-api.service.ts`
3. **NgRx SignalStore** — `{domain}/data-access/{entity}.store.ts`
4. **Smart component** — `{domain}/feature/{feature-name}/`
5. **Route registration** — `{domain}/{domain}.routes.ts`
6. **Playwright e2e** — `apps/chairly-e2e/src/{domain}.spec.ts`

All under `{FRONTEND_WT}src/frontend/chairly/libs/chairly/src/lib/{domain}/`.

## Key patterns

- Read `.claude/skills/chairly-frontend-domain/SKILL.md` for all boilerplate patterns
- All UI text in **Dutch**
- OnPush change detection, standalone components
- Signal-based APIs: `input()`, `model()`, `viewChild()`, `computed()`, `signal()`
- `signalStore` with `withState`, `withComputed`, `withMethods`
- `templateUrl:` — never inline template
- Dark mode: pair every light bg with `dark:` variant
- Entity selection via dropdowns, never raw ID inputs

## Quality gate

Run lint, format, test, and build after all tasks. Fix failures before reporting.
