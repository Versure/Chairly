# Phase 5 — Merge and Create PR

These instructions are executed directly by the lead (no subagent needed).
All paths are relative to the repo root (main checkout).

## Variables (already set from Phase 0)

- `FEATURE_NAME` — kebab-case feature name
- `BACKEND_WT` — `.worktrees/backend/`
- `FRONTEND_WT` — `.worktrees/frontend/`
- Feature branch: `feat/{FEATURE_NAME}` (already exists — created by start.sh)
- Backend branch: `impl/{FEATURE_NAME}-backend`
- Frontend branch: `impl/{FEATURE_NAME}-frontend`

## Step 1 — Commit any uncommitted changes in worktrees

```bash
# Backend worktree
cd .worktrees/backend && git add -A
git diff --cached --quiet || git commit -m "feat({FEATURE_NAME}): backend implementation"

# Frontend worktree
cd .worktrees/frontend && git add -A
git diff --cached --quiet || git commit -m "feat({FEATURE_NAME}): frontend implementation"
```

## Step 2 — Push worktree branches

```bash
cd .worktrees/backend && git push -u origin impl/{FEATURE_NAME}-backend
cd .worktrees/frontend && git push -u origin impl/{FEATURE_NAME}-frontend
```

## Step 3 — Merge worktrees into the feature branch

```bash
# Switch to the feature branch in the main checkout
git checkout feat/{FEATURE_NAME}

# Merge backend branch
git merge --no-ff impl/{FEATURE_NAME}-backend -m "chore: merge backend implementation"

# Merge frontend branch
git merge --no-ff impl/{FEATURE_NAME}-frontend -m "chore: merge frontend implementation"
```

If there are merge conflicts (unlikely since worktrees operate on separate directories),
resolve them conservatively — keep both sides if in doubt, then commit.

**Special case — `.claude/tasks/` conflicts:** If merge conflicts occur in spec or tasks files,
always keep the original Phase 0 version (from the feature branch). Discard any changes made
by implementation agents — these files should not have been modified after Phase 0.

## Step 4 — Commit the spec and tasks files

The spec and tasks.json written during Phase 0 live in the main checkout.
Add them to the feature branch commit:

```bash
git add .claude/tasks/{FEATURE_NAME}/
git diff --cached --quiet || git commit -m "chore({FEATURE_NAME}): add feature spec and tasks"
```

## Step 5 — Push the feature branch

```bash
git push -u origin feat/{FEATURE_NAME}
```

## Step 6 — Create the pull request

```bash
gh pr create \
  --title "feat({FEATURE_NAME}): {one-line summary of what was implemented}" \
  --body "$(cat <<'EOF'
## Summary

{2-3 sentence description of the feature}

## Changes

**Backend:**
- {list each B task title}

**Frontend:**
- {list each F task title}

## Quality gates

- Backend: build ✓, tests ✓, format ✓
- Frontend: lint ✓, tests ✓, build ✓, e2e ✓

## Notes

{any notable decisions, workarounds, or known limitations — or "None"}

Implemented by the feature-team agent workflow.
EOF
)" \
  --base main \
  --head feat/{FEATURE_NAME}
```

If any QA checks were still failing at the end of Phase 4, note them clearly in the
PR body under a `## Known issues` section rather than hiding them.

## Step 7 — Wait for CI/CD checks

After creating the PR, wait for CI/CD to complete:

```bash
gh run watch --exit-status
```

If CI fails:
1. Read the failure details: `gh run view --log-failed`
2. Fix the issue in the feature branch, commit, and push
3. Run `gh run watch --exit-status` again
4. Repeat up to 3 times. If still failing after 3 attempts, report the failure in the output.

## Step 8 — Report

Output the PR URL returned by `gh pr create` so the user can find it easily.

```
MERGE-COMPLETE
pr_url: {url}
feature_branch: feat/{FEATURE_NAME}
ci_status: pass | fail
```
