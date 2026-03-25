# Phase: Merge and Create PR

These instructions are executed directly by the lead (no subagent needed).
All paths are relative to the repo root (main checkout).

## Variables (set from Step 0)

- `FEATURE_NAME` — kebab-case feature name
- `BACKEND_WT` — `.worktrees/{FEATURE_NAME}/backend/`
- `FRONTEND_WT` — `.worktrees/{FEATURE_NAME}/frontend/`
- Feature branch: `feat/{FEATURE_NAME}`
- Backend branch: `impl/{FEATURE_NAME}-backend`
- Frontend branch: `impl/{FEATURE_NAME}-frontend`

## Step 1 — Commit any uncommitted changes in worktrees

Only process worktrees for active layers (determined in Step 0d).

```bash
# Backend worktree (if HAS_BACKEND or HAS_INFRA)
cd {BACKEND_WT} && git add -A
git diff --cached --quiet || git commit -m "feat({FEATURE_NAME}): backend implementation"

# Frontend worktree (if HAS_FRONTEND)
cd {FRONTEND_WT} && git add -A
git diff --cached --quiet || git commit -m "feat({FEATURE_NAME}): frontend implementation"
```

## Step 2 — Push worktree branches

```bash
# If HAS_BACKEND or HAS_INFRA:
cd {BACKEND_WT} && git push -u origin impl/{FEATURE_NAME}-backend

# If HAS_FRONTEND:
cd {FRONTEND_WT} && git push -u origin impl/{FEATURE_NAME}-frontend
```

## Step 3 — Merge worktrees into the feature branch

```bash
git checkout feat/{FEATURE_NAME}

# If HAS_BACKEND or HAS_INFRA:
git merge --no-ff impl/{FEATURE_NAME}-backend -m "chore: merge backend implementation"

# If HAS_FRONTEND:
git merge --no-ff impl/{FEATURE_NAME}-frontend -m "chore: merge frontend implementation"
```

If merge conflicts occur, resolve conservatively — keep both sides if in doubt.
For `.claude/tasks/` conflicts, keep the original spec version.

## Step 4 — Push the feature branch

```bash
git push -u origin feat/{FEATURE_NAME}
```

## Step 5 — Create the pull request

```bash
gh pr create \
  --title "feat({FEATURE_NAME}): {one-line summary}" \
  --body "$(cat <<'EOF'
## Summary

{2-3 sentence description of the feature}

## Changes

{Include only sections for active layers:}

**Backend:** (if HAS_BACKEND or HAS_INFRA)
- {list each B/I task title}

**Frontend:** (if HAS_FRONTEND)
- {list each F task title}

## Quality gates

{List only gates that were run:}
- Backend: build, tests, format (if HAS_BACKEND or HAS_INFRA)
- Frontend: lint, tests, build, e2e (if HAS_FRONTEND)

## Notes

{any notable decisions or known limitations — or "None"}

Implemented by the /implement workflow.
EOF
)" \
  --base main \
  --head feat/{FEATURE_NAME}
```

If any QA checks were still failing, note them in a `## Known issues` section.

## Step 6 — Wait for CI

```bash
gh run watch --exit-status
```

If CI fails:
1. `gh run view --log-failed`
2. Fix, commit, push
3. `gh run watch --exit-status` again
4. Repeat up to 3 times

## Step 7 — Report

Output the PR URL.

```
MERGE-COMPLETE
pr_url: {url}
feature_branch: feat/{FEATURE_NAME}
ci_status: pass | fail
```

Return to main:
```bash
git checkout main
```
