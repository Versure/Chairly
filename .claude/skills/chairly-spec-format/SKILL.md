---
name: chairly-spec-format
description: >
  Chairly feature spec format. Use when writing a feature spec and tasks.json
  for the agent team workflow. Produces spec.md (human-readable) and tasks.json
  (machine-readable task list) in .claude/tasks/{feature-name}/.
user-invocable: false
---

# Chairly Feature Spec Format

When the spec agent writes a feature spec, it produces **two files** in `.claude/tasks/{feature-name}/`:

1. `spec.md` — human-readable, full detail, single source of truth
2. `tasks.json` — machine-readable task list, minimal (title + layer + deps only)

---

## `spec.md` — Required Sections

```markdown
# {Feature Name}

## Overview

One paragraph: what this feature does and why it exists.
Reference the bounded context (e.g. Bookings, Clients, Services).

## Domain Context

- Bounded context: {context}
- Key entities involved: {Entity1}, {Entity2}
- Ubiquitous language: use terms from docs/domain-model.md

## Backend Tasks

### B1 — {Short title}

{Full description of what to build. Include:}
- Entity fields (with timestamp pairs instead of status columns)
- EF configuration notes (indexes, FKs, constraints)
- Endpoint routes and HTTP verbs
- Handler logic and validation rules
- Expected response shape
- Test cases to cover

### B2 — {Short title}

...

## Frontend Tasks

### F1 — {Short title}

{Full description of what to build. Include:}
- Model interfaces (Request/Response shapes matching backend)
- Store methods needed
- Component structure (smart vs presentational)
- UI copy in Dutch
- Route registration if new page
- Playwright e2e scenarios to cover

### F2 — {Short title}

...

## Acceptance Criteria

- [ ] {Criterion 1}
- [ ] {Criterion 2}
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Out of Scope

- {Thing explicitly not included in this iteration}
```

---

## `tasks.json` — Schema

```json
{
  "feature": "{feature-name}",
  "specPath": ".claude/tasks/{feature-name}/spec.md",
  "tasks": [
    {
      "id": "B1",
      "layer": "backend",
      "title": "{Short title matching spec.md heading}",
      "status": "pending",
      "dependsOn": []
    },
    {
      "id": "B2",
      "layer": "backend",
      "title": "{Short title}",
      "status": "pending",
      "dependsOn": ["B1"]
    },
    {
      "id": "F1",
      "layer": "frontend",
      "title": "{Short title matching spec.md heading}",
      "status": "pending",
      "dependsOn": ["B1", "B2"]
    }
  ]
}
```

### Schema Rules

- `id` — prefix `B` for backend tasks, `F` for frontend tasks, numbered sequentially
- `layer` — `"backend"` or `"frontend"` (lowercase, exact string — used by hook script)
- `title` — must match the `### {id} — {title}` heading in `spec.md` exactly
- `status` — always `"pending"` when first written; updated by agents as work progresses
- `dependsOn` — array of task IDs that must be complete before this task starts;
  frontend tasks typically depend on backend tasks that define the API contract;
  empty array `[]` means no dependencies

### Task ID Conventions

| Prefix | Layer | Example |
|---|---|---|
| `B` | backend | `B1`, `B2`, `B3` |
| `F` | frontend | `F1`, `F2` |

Number tasks in implementation order within each layer.

---

## Status Values

| Value | Meaning |
|---|---|
| `"pending"` | Not started |
| `"in_progress"` | Agent currently working on it |
| `"completed"` | Done and quality checks passed |
| `"blocked"` | Dependency not met or blocker found |

---

## Rules

- `spec.md` is the single source of truth — all detail lives there
- `tasks.json` is minimal — title and dependencies only; agents open `spec.md` for detail
- Task titles in `tasks.json` must match headings in `spec.md` exactly (used for cross-referencing)
- Spec file location: `.claude/tasks/{feature-name}/spec.md`
- Tasks file location: `.claude/tasks/{feature-name}/tasks.json`
- Feature name is kebab-case matching the git branch suffix (e.g. `bookings-crud`)
- Always include at least one backend task and one frontend task unless the feature is purely one layer
- Frontend tasks that call the backend API must declare the relevant backend tasks in `dependsOn`
