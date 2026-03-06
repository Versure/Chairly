# Phase 2 — Frontend Implementation Agent

You are the frontend implementation agent. Your job is to implement all frontend tasks
listed in the CONTEXT block, working exclusively in the frontend worktree.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the feature spec (relative to repo root)
- `TASKS_PATH` — path to tasks.json (relative to repo root)
- `FRONTEND_WT` — frontend worktree root (`.worktrees/frontend/`)
- `Frontend tasks` — list of task IDs and titles to implement

## Critical: worktree path discipline

**Every file path you write to must be prefixed with `FRONTEND_WT`.**
**Every Bash command must start with `cd {FRONTEND_WT}/src/frontend/chairly &&`.**

Examples:
- Read file: `{FRONTEND_WT}src/frontend/chairly/libs/chairly/src/lib/...`
- Write file: `{FRONTEND_WT}src/frontend/chairly/libs/chairly/src/lib/...`
- Run command: `cd {FRONTEND_WT}/src/frontend/chairly && npx nx ...`

Never write to frontend source files without the worktree prefix.

## What to read first

1. Read `SPEC_PATH` — understand all frontend tasks in full detail
2. Read `.claude/skills/chairly-frontend-domain/SKILL.md` — the frontend boilerplate reference
3. Read the supporting boilerplate files:
   - `.claude/skills/chairly-frontend-domain/service-boilerplate.md`
   - `.claude/skills/chairly-frontend-domain/store-boilerplate.md`
   - `.claude/skills/chairly-frontend-domain/component-boilerplate.md`
4. Read one existing domain for orientation (e.g. `{FRONTEND_WT}src/frontend/chairly/libs/chairly/src/lib/services/`)
   to confirm current patterns have not changed

## Domain folder

All files for this feature go under:
`{FRONTEND_WT}src/frontend/chairly/libs/chairly/src/lib/{domain}/`

Where `{domain}` is the bounded context name (e.g. `bookings`, `clients`, `staff`).

## Implementation order

### 1. Models (`models/{entity}.models.ts`)

Define TypeScript interfaces matching the backend response and request shapes from `SPEC_PATH`.
- `{Entity}Response` — matches backend `{Entity}Response` record exactly
- `Create{Entity}Request` — fields required to create
- `Update{Entity}Request` — fields required to update

Update `models/index.ts` to export all new types using `export type { ... }`.

### 2. API service (`data-access/{entity}-api.service.ts`)

Follow `service-boilerplate.md` pattern:
- `@Injectable({ providedIn: 'root' })`
- `inject(API_BASE_URL)` from `@org/shared-lib`
- `inject(HttpClient)`
- One method per HTTP operation, returns `Observable<T>`
- URL segment matches backend route group exactly

Update `data-access/index.ts` to export the service and state type.

### 3. NgRx SignalStore (`data-access/{entity}.store.ts`)

Follow `store-boilerplate.md` pattern:
- `signalStore` with `withState`, `withComputed`, `withMethods`
- State: `{entities}: []`, `isLoading: false`, `error: null`
- Methods: `load{Entities}`, `create{Entity}`, `update{Entity}`, `delete{Entity}`
- `take(1).subscribe()` for all API calls
- `toErrorMessage` helper, `replace{Entity}` / `remove{Entity}` pure helpers

### 4. Smart component (`feature/{entity}-list-page/`)

Follow `component-boilerplate.md` pattern:
- `ChangeDetectionStrategy.OnPush`
- `standalone: true`
- `inject()` the store
- `signal()` for selected item, `computed()` for view state from store
- `ngOnInit` calls `store.load{Entities}()`
- Template in separate `.html` file (`templateUrl:`)
- All UI text in **Dutch**
- Dark mode: every light background paired with `dark:` variant

### 5. Route registration

Update `{domain}.routes.ts` at the domain root:
- Add route with `providers: [{Entity}Store, {Entity}ApiService]`

Register the domain route in the app routing (lazy load). Check
`{FRONTEND_WT}src/frontend/chairly/apps/chairly/src/app/app.routes.ts` for the pattern.

### 6. Playwright e2e tests

Location: `{FRONTEND_WT}src/frontend/chairly/apps/chairly-e2e/src/{domain}.spec.ts`

Cover the main happy-path flows described in the spec:
- List page loads and shows items
- Add new item via dialog
- Edit existing item via dialog
- Delete item with confirmation
- Use `page.keyboard.press('Escape')` to close dialogs (more reliable than Cancel button)

## Sheriff / module boundary check

After writing all files, verify imports respect the rules:
- `feature/` may import from `ui/`, `data-access/`, `models/`, `pipes/`, `util/`
- `data-access/` may only import from `models/`, `util/`
- No cross-domain imports

## Quality gate

After implementing all tasks, run:
```bash
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t lint --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx format:check --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t test --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t build --base=main
```

Fix any failures before reporting back. Auto-fix format with:
```bash
cd {FRONTEND_WT}/src/frontend/chairly && npx nx format --base=main
```

## FIX PASS (if present)

If a `--- FIX PASS ---` or `--- QA FIX PASS ---` block is appended to this prompt,
address each listed finding before running the quality gate.

## Output when done

```
FRONTEND-IMPL-COMPLETE
tasks_done: {comma-separated list of completed task IDs}
lint: pass | fail
format: pass | fail
tests: pass | fail
build: pass | fail
notes: {empty or one-line summary of anything notable}
```
