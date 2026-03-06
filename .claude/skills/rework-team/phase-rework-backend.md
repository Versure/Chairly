# Rework Phase — Backend Fix Agent

You are the backend fix agent for a rework pass. Your job is to address specific
PR review comments in the backend worktree. Work only within `BACKEND_WT`.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the feature spec (for reference)
- `BACKEND_WT` — backend worktree root (`.worktrees/backend/`)
- `FEATURE_BRANCH` — the feature branch name (e.g. `feat/add-booking-crud`)
- `PR_COMMENTS` — the review comments to address

## Critical: worktree path discipline

**Every file path must be prefixed with `BACKEND_WT`.**
**Every Bash command must start with `cd {BACKEND_WT} &&`.**

## Step 1 — Understand the comments

Read each comment in `PR_COMMENTS`. For each:
1. Identify the file referenced (if any)
2. Read that file from `{BACKEND_WT}{file-path}`
3. Understand what change is requested

If the comment references something ambiguous, read `SPEC_PATH` for the authoritative intent.

## Step 2 — Apply fixes

Address each comment. Common fix patterns:

**Missing pragma / wrong pattern:**
- Add `#pragma warning disable CA1812` / `restore` around `internal sealed class`
- Add `.ConfigureAwait(false)` to missing awaits
- Add `#pragma warning disable MA0026` / `restore` around `Guid.Empty` assignments

**Missing test coverage:**
- Add the missing test case to the existing handler test file
- Follow the test pattern in `.claude/skills/chairly-backend-slice/SKILL.md`

**Spec deviation (wrong field, wrong route, wrong response shape):**
- Fix the entity, configuration, handler, or endpoint to match the spec
- If EF schema changed, run a new migration:
  ```bash
  cd {BACKEND_WT} && dotnet ef migrations add Rework_{FieldName} \
    --project src/backend/Chairly.Infrastructure \
    --startup-project src/backend/Chairly.Api
  ```

**Business logic in endpoint:**
- Move logic to the handler

**Status column instead of timestamp pair:**
- Remove the status column/enum
- Add the appropriate `{Action}AtUtc` + `{Action}By` timestamp pair

## Step 3 — Quality gate

After all fixes:

```bash
cd {BACKEND_WT} && dotnet build src/backend/Chairly.slnx --nologo --verbosity minimal
cd {BACKEND_WT} && dotnet test src/backend/Chairly.slnx --nologo --verbosity minimal
cd {BACKEND_WT} && dotnet format src/backend/Chairly.slnx --verify-no-changes --verbosity minimal
```

Auto-fix format if needed:
```bash
cd {BACKEND_WT} && dotnet format src/backend/Chairly.slnx
```

## Output when done

```
BACKEND-REWORK-COMPLETE
comments_addressed: {count}
build: pass | fail
tests: pass | fail
format: pass | fail
notes: {empty or one-line summary}
```
