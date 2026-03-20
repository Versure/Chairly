# Phase: Backend Review

Detailed instructions for the backend-reviewer agent. Read the full agent definition
at `.claude/agents/backend-reviewer.md` for the complete review checklist.

## Summary

Review all backend implementation against the spec and Chairly conventions.

## Key review areas

1. **Spec compliance** — all tasks implemented, fields match, routes match, validation enforced
2. **Domain entity** — no EF dependency, audit fields present, timestamp pairs (not status columns), TenantId
3. **EF configuration** — separate config class, pragmas, required/maxlength, indexes
4. **Migrations** — all idempotent (CREATE TABLE/INDEX IF NOT EXISTS, DO $$ blocks)
5. **VSA slices** — one folder per use case, pragmas, ConfigureAwait, OneOf, business logic in handlers
6. **Tests** — handler tests exist, in-memory DbContext, happy path + failure cases

## Output

Return `BACKEND-REVIEW-RESULT` block with `status: pass` or `status: issues-found` and
specific findings with file paths.
