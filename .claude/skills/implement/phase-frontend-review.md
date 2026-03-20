# Phase: Frontend Review

Detailed instructions for the frontend-reviewer agent. Read the full agent definition
at `.claude/agents/frontend-reviewer.md` for the complete review checklist.

## Summary

Review all frontend implementation against the spec and Chairly conventions.

## Key review areas

1. **Spec compliance** — all tasks implemented, models match backend, Dutch UI text, e2e coverage
2. **File structure** — Sheriff rules, models in `models/`, pipes in `pipes/`, components in subfolders
3. **Component conventions** — OnPush, standalone, inject(), signals, templateUrl, no empty imports
4. **Store conventions** — signalStore, withState/withComputed/withMethods, take(1), patchState
5. **API service** — providedIn root, inject API_BASE_URL, Observable returns
6. **Styling** — Tailwind only, dark mode variants, @if/@for control flow
7. **TypeScript** — no any, no console, explicit return types
8. **E2e tests** — Playwright tests exist, Escape to close dialogs, happy paths covered

## Output

Return `FRONTEND-REVIEW-RESULT` block with `status: pass` or `status: issues-found` and
specific findings with file paths.
