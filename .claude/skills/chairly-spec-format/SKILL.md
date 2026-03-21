---
name: chairly-spec-format
description: >
  Chairly feature spec format for Claude Code agent workflows. Specs are written to
  .claude/tasks/{feature-name}/spec.md + tasks.json.
user-invocable: false
---

# Chairly Feature Spec Format

## Output location

- Spec: `.claude/tasks/{feature-name}/spec.md`
- Tasks: `.claude/tasks/{feature-name}/tasks.json`

When the Claude workflow writes a feature spec, it produces **two files** in `.claude/tasks/{feature-name}/`:

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

## Infrastructure Tasks (if applicable)

### I1 — {Short title}

{Full description of what to configure/set up. Include:}
- Which infrastructure component (Aspire, Keycloak, RabbitMQ, SMTP, seeding)
- Files to modify (AppHost, Program.cs, appsettings, etc.)
- Configuration values and their sources
- DI registration details
- Health check requirements
- Test cases to cover

### I2 — {Short title}

...

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
- Entity selection: forms that reference related entities (client, staff, service) must use dropdowns or autocomplete, NEVER raw ID inputs. Specify which entities need to be loaded for selection and how they are fetched.
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
      "id": "I1",
      "layer": "infra",
      "title": "{Short title matching spec.md heading}",
      "status": "pending",
      "dependsOn": []
    },
    {
      "id": "B1",
      "layer": "backend",
      "title": "{Short title matching spec.md heading}",
      "status": "pending",
      "dependsOn": ["I1"]
    },
    {
      "id": "F1",
      "layer": "frontend",
      "title": "{Short title matching spec.md heading}",
      "status": "pending",
      "dependsOn": ["B1"]
    }
  ]
}
```

Note: `I` (infra) tasks are optional. Only include them when the feature requires
infrastructure changes (Aspire setup, Keycloak config, RabbitMQ topology, SMTP, seeding).
Most features will only have `B` and `F` tasks.

### Schema Rules

- `id` — prefix `I` for infra tasks, `B` for backend tasks, `F` for frontend tasks, numbered sequentially
- `layer` — `"infra"`, `"backend"`, or `"frontend"` (lowercase, exact string)
- `title` — must match the `### {id} — {title}` heading in `spec.md` exactly
- `status` — always `"pending"` when first written; updated by agents as work progresses
- `dependsOn` — array of task IDs that must be complete before this task starts;
  frontend tasks typically depend on backend tasks that define the API contract;
  empty array `[]` means no dependencies

### Task ID Conventions

| Prefix | Layer | Example |
|---|---|---|
| `I` | infra | `I1`, `I2` |
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

## Special: Marketing / Landing Pages

When a feature includes a public-facing marketing or landing page, the spec must include:

- **Content sections:** List all content sections with their Dutch headings and purpose
  (hero, features/USPs, social proof, CTA, etc.)
- **Distinct visuals:** When multiple items of the same type are shown (e.g. feature cards, USPs),
  each item MUST have a visually distinct representation (different icon, image, or illustration).
  Specify which icon/visual to use for each item — do not leave this to the implementer.
- **SEO requirements:** meta title, description, keywords (in Dutch), Open Graph tags, `lang="nl"`,
  proper heading hierarchy (h1 → h2 → h3)
- **Marketing copy depth:** Include enough content to be convincing — not just feature names, but
  benefit descriptions, social proof with placeholder stats, and clear CTAs
- **Text contrast:** Specify that all text must have sufficient contrast against its background
  (WCAG AA minimum). Body text should use `text-gray-700` or darker on light backgrounds.

---

## Rules

- `spec.md` is the single source of truth — all detail lives there
- `tasks.json` is minimal — title and dependencies only; agents open `spec.md` for detail
- Task titles in `tasks.json` must match headings in `spec.md` exactly (used for cross-referencing)
- Spec file location: `.claude/tasks/{feature-name}/spec.md`
- Tasks file location: `.claude/tasks/{feature-name}/tasks.json`
- Feature name is kebab-case matching the git branch suffix (e.g. `bookings-crud`)
- Always include at least one backend task and one frontend task unless the feature is purely one layer
- Infra tasks are optional — only include when the feature requires infrastructure changes
- Backend tasks that depend on infrastructure setup must declare the relevant infra tasks in `dependsOn`
- Frontend tasks that call the backend API must declare the relevant backend tasks in `dependsOn`
