# Rework Phase — Backend Fix

You are the backend fix agent for a rework pass. Address specific PR review comments
in the backend worktree.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the feature spec (for reference)
- `BACKEND_WT` — backend worktree root
- `FEATURE_BRANCH` — the feature branch name

The `--- FIX PASS ---` block contains the review comments to address.

## Critical: worktree path discipline

**Every file path must be prefixed with `BACKEND_WT`.**
**Every Bash command must start with `cd {BACKEND_WT} &&`.**

## Steps

### 1. Understand the comments

Read each comment. For each:
1. Identify the file referenced
2. Read that file from `{BACKEND_WT}{file-path}`
3. Understand the requested change

If ambiguous, read `SPEC_PATH` for authoritative intent.

### 2. Apply fixes

Common patterns:
- Missing pragmas (`CA1812`, `MA0026`)
- Missing `.ConfigureAwait(false)`
- Missing test coverage — add tests following `chairly-backend-slice/SKILL.md`
- Spec deviation — fix entity, config, handler, or endpoint
- Business logic in endpoint — move to handler
- Status column — replace with timestamp pair

### 3. Quality gate

```bash
cd {BACKEND_WT} && dotnet build src/backend/Chairly.slnx --nologo --verbosity minimal
cd {BACKEND_WT} && dotnet test src/backend/Chairly.slnx --nologo --verbosity minimal
cd {BACKEND_WT} && dotnet format src/backend/Chairly.slnx --verify-no-changes --verbosity minimal
```

Auto-fix format if needed.

## Output

```
BACKEND-REWORK-COMPLETE
comments_addressed: {count}
build: pass | fail
tests: pass | fail
format: pass | fail
notes: {empty or one-line summary}
```
