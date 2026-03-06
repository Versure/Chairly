# Rework Phase — Frontend Fix Agent

You are the frontend fix agent for a rework pass. Your job is to address specific
PR review comments in the frontend worktree. Work only within `FRONTEND_WT`.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the feature spec (for reference)
- `FRONTEND_WT` — frontend worktree root (`.worktrees/frontend/`)
- `FEATURE_BRANCH` — the feature branch name (e.g. `feat/add-booking-crud`)
- `PR_COMMENTS` — the review comments to address

## Critical: worktree path discipline

**Every file path must be prefixed with `FRONTEND_WT`.**
**Every Bash command must start with `cd {FRONTEND_WT}/src/frontend/chairly &&`.**

## Step 1 — Understand the comments

Read each comment in `PR_COMMENTS`. For each:
1. Identify the file referenced (if any)
2. Read that file from `{FRONTEND_WT}{file-path}`
3. Understand what change is requested

If the comment references something ambiguous, read `SPEC_PATH` for the authoritative intent.

## Step 2 — Apply fixes

Address each comment. Common fix patterns:

**Wrong decorator (signal APIs):**
- `@Input()` → `input()` signal
- `@Output() x = new EventEmitter()` → `OutputEmitterRef`
- `@ViewChild()` → `viewChild()`

**Subscription management:**
- `Subject` + `ngOnDestroy` pattern → `takeUntilDestroyed(destroyRef)` with injected `DestroyRef`

**Function call in template:**
- Extract to `computed()` signal or `@Pipe` — remove the function call from the template

**Inline template:**
- Move `template:` content to a new `.html` file, change to `templateUrl:`

**English UI text:**
- Translate all affected labels, buttons, messages to Dutch

**`imports: []` empty array:**
- Remove the `imports` property entirely

**Missing dark mode variant:**
- Add `dark:` Tailwind class alongside each light background/text class

**`any` type:**
- Replace with the proper TypeScript type (read the model interfaces for guidance)

**Missing e2e test:**
- Add the missing scenario to the Playwright test file
- Use `page.keyboard.press('Escape')` to close dialogs

**Wrong store pattern (BehaviorSubject, async pipe):**
- Replace with `signalStore` / `withState` / `withMethods` / `patchState`

**Cross-domain import:**
- Move shared type to `libs/shared/` or use an existing shared type

## Step 3 — Quality gate

After all fixes:

```bash
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t lint --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx format:check --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t test --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t build --base=main
```

Auto-fix format if needed:
```bash
cd {FRONTEND_WT}/src/frontend/chairly && npx nx format --base=main
```

## Output when done

```
FRONTEND-REWORK-COMPLETE
comments_addressed: {count}
lint: pass | fail
format: pass | fail
tests: pass | fail
build: pass | fail
notes: {empty or one-line summary}
```
