# Agent Workflow Optimization Plan

## Scope
- `.claude/agents/` and `.claude/skills/`
- `scripts/agent-team/` and docs under `docs/agent-teams-*`
- Regression evals for agent workflow config

## Current State (Observations)
- `docs/agent-teams-workflow.md` drifted from the live skill and script layout.
- `scripts/agent-team/start.sh` was referenced but missing.
- No automated validation of `tasks.json` schema or agent/skill front matter.

## Goals
- Single source of truth for spec/task paths.
- Keep scripts, skills, and docs consistent.
- Catch regressions early with evals and CI.

## Plan
1. Align docs and skill guidance with the live file layout and spec location.
2. Add `scripts/agent-team/start.sh` as the canonical entrypoint.
3. Add an evals script and CI workflow to validate config on PRs.
4. Decide on long-term spec storage strategy: keep in `.claude/tasks/` or sync to `docs/specs/`.
5. Add a small golden-path fixture spec/tasks to smoke-test the workflow.
6. Add prompt versioning and a changelog for skill updates.

## Evals
- Run locally: `./scripts/agent-team/evals/run.sh`
- CI: `agent-workflow-evals` workflow on changes to `.claude/`, `scripts/agent-team/`, or `docs/agent-teams-*.md`

## This Branch
- Updated docs and spec format to the `.claude/tasks/` spec location.
- Added `scripts/agent-team/start.sh`.
- Added evals script and CI workflow.
