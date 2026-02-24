# AI Workflow — Ralph Autonomous Development

This document describes the detailed process for using Ralph to implement features autonomously. For the decision rationale, see [ADR-010](adr/ADR-010-ai-first-development.md).

---

## Overview

Ralph is an autonomous agent loop that runs Claude Code in WSL. It reads a PRD (Product Requirements Document) in JSON format, picks the highest-priority incomplete story, and implements it in a fresh Claude Code session. After each story, it runs quality checks and commits if they pass.

## Environments

```
Windows (interactive work):
  C:\Projects\Prive\Chairly\Chairly\     ← Architect works here with Claude Code

WSL (autonomous agent):
  ~/projects/Chairly/                      ← Ralph's clone, runs autonomously
```

Both point to the same GitHub repository. Coordinate via branches and PRs.

---

## Feature Implementation Flow

```
1. Architect writes spec          →  docs/specs/{feature-name}.md
2. Architect converts to PRD      →  scripts/ralph/prd.json
3. Architect starts Ralph in WSL  →  ./scripts/ralph/ralph.sh --tool claude
4. Ralph implements stories       →  One story per Claude Code session
5. Ralph runs quality checks      →  Build, test, lint after each story
6. Ralph commits passing stories  →  Conventional commit messages
7. Architect reviews branch       →  GitHub PR review
8. Architect merges or requests   →  Changes via PR comments
```

---

## PRD Format

Each feature is described as a `prd.json` with granular user stories:

```json
{
  "project": "Chairly",
  "branchName": "ralph/{feature-name}",
  "description": "Feature Name — Short description",
  "userStories": [
    {
      "id": "XX-001",
      "title": "Short imperative title",
      "description": "Detailed description of what to implement.",
      "acceptanceCriteria": [
        "Criterion 1 — specific and testable",
        "Criterion 2 — specific and testable"
      ],
      "priority": 1,
      "passes": false,
      "notes": "Dependencies, context, or hints for Claude Code"
    }
  ]
}
```

### Story Sizing Guidelines

**Right-sized (one Claude Code context):**
- Add a database entity and migration
- Create a CQRS command/query handler with validation
- Add an API endpoint for an existing handler
- Build a single UI component
- Add a filter or sorting feature to a list
- Write tests for an existing handler

**Too large (split these):**
- "Build the entire dashboard"
- "Add authentication"
- "Implement booking management" (split: entity → handler → endpoint → UI)
- Any story touching more than 2-3 files in different layers

---

## Running Ralph

```bash
# SSH into WSL
wsl

# Navigate to project and pull latest
cd ~/projects/Chairly
git pull

# Start Ralph (default 10 iterations)
./scripts/ralph/ralph.sh --tool claude

# Start with custom iteration count
./scripts/ralph/ralph.sh --tool claude 20

# Monitor progress
cat scripts/ralph/progress.txt
cat scripts/ralph/prd.json | jq '.userStories[] | {id, title, passes}'
```

---

## Quality Checks

Ralph runs these checks after every story iteration:

**Backend:**
- `dotnet build` — compilation
- `dotnet test` — unit + integration tests
- `dotnet format --verify-no-changes` — formatting

**Frontend:**
- `nx lint` — ESLint
- `nx test` — unit tests

If checks fail, Ralph attempts to fix the issues in the same iteration before committing.

---

## Ralph's File Structure

```
scripts/ralph/
├── ralph.sh          # The loop script
├── CLAUDE.md         # Ralph's instructions to Claude Code
├── prd.json          # Current feature tasks (created per feature)
├── progress.txt      # Append-only memory between iterations
├── .last-branch      # Branch tracking
└── archive/          # Previous runs (auto-archived)
```

---

## Definition of Done (Per Feature)

- [ ] All user stories in prd.json have `passes: true`
- [ ] All quality checks pass
- [ ] Code reviewed by architect
- [ ] Conventional commit messages used
- [ ] Feature branch merged to main
- [ ] progress.txt documents any decisions Ralph made
