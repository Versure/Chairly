# ADR-010: AI-First Development with Ralph

## Status

Accepted

## Context

Chairly is built by a small team where the architect/product owner defines features and an AI agent implements them. We need a structured workflow that enables autonomous AI-driven development without sacrificing code quality.

## Decision

We adopt an **AI-first development approach** using **Ralph** (an autonomous agent loop running Claude Code) for feature implementation.

### Two Modes of Development

1. **Interactive mode** — The architect works directly with Claude Code for architecture, decision-making, and setup. Claude Code asks questions and waits for decisions.

2. **Autonomous mode (Ralph)** — Ralph runs Claude Code in a loop in WSL, implementing features from PRDs without human intervention. Ralph reads `CLAUDE.md`, follows established patterns, and commits working code.

### Workflow

1. Architect writes a feature spec in `docs/specs/`
2. Spec is converted to a `prd.json` with granular, right-sized user stories
3. Ralph picks up the PRD and implements stories one by one
4. Each story is implemented in a fresh Claude Code context
5. Quality checks run after each story (build, test, lint)
6. Architect reviews the branch and merges or requests changes

### Documentation Requirements

For this workflow to succeed, the codebase must be exceptionally well-documented:
- `CLAUDE.md` contains all conventions, patterns, and instructions
- `docs/domain-model.md` defines the ubiquitous language
- `docs/adr/` records all architecture decisions
- `docs/specs/` contains detailed feature specifications
- Existing code serves as the primary example for new code

### Detailed Process

See `docs/ai-workflow.md` for the complete Ralph workflow, PRD format, and operational procedures.

## Consequences

- **Positive:** High development velocity — Ralph can work around the clock on well-defined stories.
- **Positive:** Consistent code quality — Ralph follows the same patterns every time (no "Friday afternoon code").
- **Positive:** Forces excellent documentation — if it's not documented, Ralph can't follow it.
- **Positive:** Architect focuses on high-value work (design, review) instead of boilerplate implementation.
- **Negative:** Requires investment in documentation and spec writing upfront.
- **Negative:** Complex or ambiguous tasks may produce poor results — story sizing is critical.
- **Negative:** Ralph cannot make architectural decisions — it follows patterns, it doesn't create them.
