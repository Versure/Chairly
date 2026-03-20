# Rework Phase — Frontend Fix

You are the frontend fix agent for a rework pass. Address specific PR review comments
in the frontend worktree.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the feature spec (for reference)
- `FRONTEND_WT` — frontend worktree root
- `FEATURE_BRANCH` — the feature branch name

The `--- FIX PASS ---` block contains the review comments to address.

## Critical: worktree path discipline

**Every file path must be prefixed with `FRONTEND_WT`.**
**Every Bash command must start with `cd {FRONTEND_WT}/src/frontend/chairly &&`.**

## Steps

### 1. Understand the comments

Read each comment. For each:
1. Identify the file referenced
2. Read that file from `{FRONTEND_WT}{file-path}`
3. Understand the requested change

If ambiguous, read `SPEC_PATH` for authoritative intent.

### 2. Apply fixes

Common patterns:
- Wrong decorator → signal-based API (`input()`, `model()`, `viewChild()`)
- `Subject` + `ngOnDestroy` → `takeUntilDestroyed(destroyRef)`
- Function call in template → `computed()` signal or `@Pipe`
- Inline template → move to `.html` file with `templateUrl:`
- English UI text → translate to Dutch
- `imports: []` → remove property entirely
- Missing dark mode → add `dark:` variant
- `any` type → proper TypeScript type
- Missing e2e test → add Playwright test
- Wrong store pattern → `signalStore` / `withState` / `withMethods`
- Cross-domain import → move to `libs/shared/`

### 3. Quality gate

```bash
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t lint --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx format:check --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t test --base=main
cd {FRONTEND_WT}/src/frontend/chairly && npx nx affected -t build --base=main
```

Auto-fix format if needed.

## Output

```
FRONTEND-REWORK-COMPLETE
comments_addressed: {count}
lint: pass | fail
format: pass | fail
tests: pass | fail
build: pass | fail
notes: {empty or one-line summary}
```
