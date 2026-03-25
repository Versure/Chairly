---
name: cleanup
description: >
  Repository housekeeping — prunes merged branches, removes orphaned task files,
  and cleans stale worktrees. Usage: /cleanup.
  Use this skill when the user wants to clean up the repo, remove old branches,
  prune stale files, or says things like "clean up", "housekeeping", "remove old branches",
  or "tidy up the repo".
user-invocable: true
---

# /cleanup — Repository Housekeeping

You perform repository housekeeping tasks. Run each section and report what was cleaned up.

---

## Step 1 — Prune merged branches

Find and remove local branches that have been merged into `main`:

```bash
# List merged branches (excluding main and current)
git branch --merged main | grep -v -E '^\*|main$' | sed 's/^[ \t]*//'
```

For each merged branch found:
- Show the branch name and its last commit message
- Delete with `git branch -d {branch}`

Also prune remote tracking branches:
```bash
git fetch --prune
```

Then check for remote branches that are merged:
```bash
gh pr list --state merged --json headRefName --jq '.[].headRefName'
```

For branches that are merged on remote but still exist locally, delete them.

**Safety:** Never delete `main`. Never force-delete (`-D`) — only use `-d` (safe delete).

---

## Step 2 — Remove orphaned task files

Find `tasks.json` files that reference spec paths that don't exist:

```bash
# For each tasks.json, check if specPath target exists
for f in .claude/tasks/*/tasks.json; do
  spec=$(jq -r '.specPath // empty' "$f" 2>/dev/null)
  [ -n "$spec" ] && [ ! -f "$spec" ] && echo "ORPHANED: $f (references $spec)"
done
```

Also find task directories that have `tasks.json` but no `spec.md`:

```bash
for dir in .claude/tasks/*/; do
  [ -f "${dir}tasks.json" ] && [ ! -f "${dir}spec.md" ] && echo "MISSING SPEC: $dir"
done
```

For each orphaned file found, show:
- The task file path
- The missing spec path it references
- Ask the user for confirmation before deleting

---

## Step 3 — Clean stale worktrees

List all registered worktrees and check for stale ones:

```bash
git worktree list
git worktree prune --dry-run
```

Prune stale worktree entries:
```bash
git worktree prune
```

Also check for leftover `.worktrees/` directories that aren't registered:
```bash
# Find directories in .worktrees/ that git doesn't know about
for dir in .worktrees/*/*; do
  [ -d "$dir" ] && ! git worktree list | grep -q "$dir" && echo "UNREGISTERED: $dir"
done
```

Remove unregistered worktree directories after confirming with the user.

**After cleanup, return to repo root:**
```bash
cd "$(git rev-parse --show-toplevel)"
```

---

## Step 4 — Report

Summarize what was cleaned:

```
CLEANUP-COMPLETE
branches_deleted: {count}
orphaned_tasks: {count removed}
worktrees_pruned: {count}
notes: {any items skipped or needing manual attention}
```

---

## Global rules

- Always confirm with the user before deleting anything
- Never force-delete branches
- Never delete `main` or any currently checked-out branch
- Show what will be deleted before doing it
- If nothing needs cleaning, report that the repo is clean
