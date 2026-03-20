# ADR-010: AI-First Development with Claude Code

## Status

Accepted (revised 2026-03-20)

## Context

Chairly is built by a small team where the architect/product owner defines features and an AI agent implements them. We need a structured workflow that enables AI-driven development without sacrificing code quality.

Previously, we used Ralph (an autonomous agent loop) for implementation. This has been replaced by a structured Claude Code workflow with specialized agents and skills.

## Decision

We adopt an **AI-first development approach** using **Claude Code** with a structured agent team workflow for feature implementation.

### Development Mode

The architect works with Claude Code interactively. Claude Code orchestrates specialized agents for spec writing, implementation, review, and quality assurance.

### Workflow

1. Architect creates a feature spec via `/create-spec` — an interactive process where Claude Code's spec-writer agent interviews the architect and proposes decisions
2. Spec is reviewed automatically by a spec-reviewer agent, then submitted as a PR for human review
3. After spec PR is merged, `/implement` spawns parallel backend and frontend agents in isolated git worktrees
4. Code is automatically reviewed by reviewer agents and validated by QA agents
5. A PR is created for human review; rework cycles are handled via `/rework-code`

### Agent Architecture

- **Spec agents** — spec-writer (Opus, interactive) and spec-reviewer (Sonnet, read-only)
- **Infrastructure agents** — infra-impl (Opus, full tools) and infra-reviewer (Sonnet, read-only)
- **Implementation agents** — backend-impl and frontend-impl (Opus, full tools, worktree isolation)
- **Review agents** — backend-reviewer and frontend-reviewer (Sonnet, read-only)
- **QA agents** — chairly-backend-qa and chairly-frontend-qa (Sonnet, build/test/format)
- **Explorer agent** — chairly-explorer (Haiku, read-only codebase lookups)

### Documentation Requirements

For this workflow to succeed, the codebase must be well-documented:
- `CLAUDE.md` contains all conventions, patterns, and instructions
- `docs/domain-model.md` defines the ubiquitous language
- `docs/adr/` records all architecture decisions
- `.claude/skills/` contains pattern references (backend slices, frontend domains, spec format)
- Existing code serves as the primary example for new code

### Detailed Process

See `docs/ai-workflow.md` for the complete workflow, agent definitions, and operational procedures.

## Consequences

- **Positive:** High development velocity — parallel backend/frontend implementation with automated review and QA
- **Positive:** Consistent code quality — agents follow the same patterns every time
- **Positive:** Forces excellent documentation — agents need clear conventions to follow
- **Positive:** Architect focuses on high-value work (design, review) instead of boilerplate
- **Positive:** Interactive spec creation catches issues early, before implementation
- **Positive:** Isolated worktrees prevent backend/frontend conflicts
- **Negative:** Requires investment in documentation, spec writing, and agent definitions upfront
- **Negative:** Complex or ambiguous tasks may produce poor results — spec quality is critical
- **Negative:** Agents cannot make architectural decisions — they follow patterns, they don't create them
