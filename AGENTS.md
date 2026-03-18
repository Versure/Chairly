# Chairly — Agent Instructions

You are working on the Chairly multi-tenant SaaS platform for salons and barbershops.

## Workflow

This project uses a structured agentic workflow with specialized agents. Follow these phases when implementing features end-to-end:

### Phase 1 — Specification

Use the `spec-writer` agent interactively to create a feature spec. The spec is written to `.github/tasks/{feature-name}/spec.md` with YAML frontmatter for task tracking and Markdown body for human review.

### Phase 2 — Implementation

Use `/fleet` to parallelize work across the `backend-dev` and `frontend-dev` agents:

```
/fleet Implement the feature spec at .github/tasks/{feature-name}/spec.md.
Use the backend-dev agent for all backend tasks and the frontend-dev agent for all frontend tasks.
Set up git worktrees for isolation:
  - .worktrees/{feature-name}/backend/ on branch impl/{feature-name}-backend
  - .worktrees/{feature-name}/frontend/ on branch impl/{feature-name}-frontend
Both branch from feat/{feature-name}.
```

### Phase 3 — Review

Use the `reviewer` agent to review the implementation. It auto-fixes what it can and writes remaining findings to `.github/tasks/{feature-name}/review.md`.

### Phase 4 — Pull Request

After review, merge worktree branches into the feature branch and create a PR:

```bash
git checkout feat/{feature-name}
git merge --no-ff impl/{feature-name}-backend -m "chore: merge backend implementation"
git merge --no-ff impl/{feature-name}-frontend -m "chore: merge frontend implementation"
git push -u origin feat/{feature-name}
gh pr create --title "feat({context}): {description}" --body "See .github/tasks/{feature-name}/spec.md"
```

### Phase 5 — Rework

After PR review, use the `rework` agent to fix review comments:

```
copilot --agent=rework --prompt "fix PR #{number} comments"
```

## Git Branching

```
main
 └── feat/{feature-name}           ← PR target
      ├── impl/{feature-name}-backend    ← worktree
      └── impl/{feature-name}-frontend   ← worktree
```

## Key References

- Domain model: `docs/domain-model.md`
- Architecture decisions: `docs/adr/`
- Feature specs: `.github/tasks/{feature-name}/spec.md`
- Project conventions: `.github/copilot-instructions.md`
- Backend patterns: `.github/instructions/backend.instructions.md`
- Frontend patterns: `.github/instructions/frontend.instructions.md`

## Autonomous Mode

When running in autopilot (`--autopilot --yolo`):
- Do NOT stop to ask questions — make decisions based on existing patterns, ADRs, and specs
- Follow conventions established in the codebase
- When in doubt, choose the simplest approach that follows existing patterns
- Always run quality checks before committing
- Use conventional commits: `feat(context): description`
