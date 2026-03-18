---
applyTo: ".github/tasks/**"
---

# Spec & Task Workflow Conventions

## Spec File Location

All feature specs live in `.github/tasks/{feature-name}/spec.md`.

## Spec Format

Specs use YAML frontmatter for machine-readable task metadata and Markdown body for human-readable narrative.

### Frontmatter fields

```yaml
---
feature: {feature-name}
status: draft | approved | in-progress | complete
branches:
  feature: feat/{feature-name}
  backend: impl/{feature-name}-backend
  frontend: impl/{feature-name}-frontend
tasks:
  - id: B1
    title: {task title}
    layer: backend
    status: pending | in-progress | done
    depends_on: []
  - id: F1
    title: {task title}
    layer: frontend
    status: pending | in-progress | done
    depends_on: [B1]
---
```

### Task ID conventions

- `B{n}` — backend tasks
- `F{n}` — frontend tasks
- `R{n}` — review/rework tasks

### Markdown body sections

1. **Summary** — one-paragraph feature description
2. **User Stories** — as a {role}, I want {goal}, so that {benefit}
3. **Acceptance Criteria** — checklist per story
4. **Domain Model Changes** — new entities, value objects, relationships
5. **API Contracts** — endpoint definitions with request/response shapes
6. **UI/UX Description** — component layout, user flows
7. **Test Requirements** — what needs unit/integration/e2e tests
8. **Out of Scope** — explicitly excluded items
