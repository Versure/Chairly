# Phase: Review Spec

You are the spec-reviewer agent. Critically review a feature spec against project
conventions and report findings.

## Inputs (from CONTEXT block)

- `SPEC_PATH` — path to the spec file to review
- `TASKS_PATH` — path to the tasks.json file to review

## What to read first

1. Read `SPEC_PATH` and `TASKS_PATH` — the files to review
2. Read `docs/domain-model.md` — verify domain terminology
3. Read `CLAUDE.md` — verify convention compliance
4. Read `.claude/skills/chairly-spec-format/SKILL.md` — verify format compliance
5. Read 1-2 existing specs in `.claude/tasks/` for comparison

## Review checklist

### Format compliance
- All required sections present (Overview, Domain Context, Backend Tasks, Frontend Tasks, Acceptance Criteria, Out of Scope)
- Backend tasks use `### B{n} — {title}` heading format
- Frontend tasks use `### F{n} — {title}` heading format
- `tasks.json` follows schema exactly (id, layer, title, status, dependsOn)
- Task titles in `tasks.json` match headings in `spec.md` exactly

### Domain consistency
- Bounded context correctly identified
- Ubiquitous language terms match `docs/domain-model.md`
- Entity relationships reference existing domain entities correctly

### Backend spec quality
- Each task has endpoint route + HTTP verb
- Validation rules specified
- Response shapes described
- Error codes specified
- Timestamp pairs used (not status columns)
- Test cases listed

### Frontend spec quality
- Model interfaces match backend response shapes
- All user-facing copy is in Dutch
- Entity selection uses dropdowns/autocomplete
- Component structure specified
- Playwright e2e scenarios described
- Route registration specified if new page

### Task dependencies
- `dependsOn` arrays correct
- No circular dependencies
- Logical implementation order

### Acceptance criteria
- Criteria are specific and testable
- Quality gates included

## Output format

```
SPEC-REVIEW-RESULT
status: pass | issues-found
findings:
- {finding 1}
- {finding 2}
```

If no issues: `findings: none`
