# ADR-001: Monorepo with Nx

## Status

Accepted

## Context

Chairly consists of a .NET backend and an Angular frontend. We need to decide how to organize the codebase: separate repositories per project, or a single monorepo.

## Decision

We use a **monorepo** managed by **Nx**.

- The .NET backend solution lives in `src/backend/`
- The Angular frontend workspace lives in `src/frontend/chairly-app/`
- Shared documentation, scripts, and configuration live at the root

Nx provides:
- Task orchestration (`nx build`, `nx test`, `nx lint`) across both stacks
- Dependency graph and affected commands (only rebuild/test what changed)
- Consistent tooling for CI/CD

## Consequences

- **Positive:** Single source of truth, atomic commits across frontend and backend, shared CI pipeline, easier onboarding.
- **Positive:** Nx affected commands keep CI fast even as the repo grows.
- **Negative:** Nx adds a learning curve and configuration overhead.
- **Negative:** .NET and Angular have different build systems — Nx orchestrates but does not replace `dotnet` CLI.
