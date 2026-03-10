# Dropdown Chevron Indent

## Overview

Native `<select>` dropdown elements in the app have their browser-rendered chevron/arrow flush against the right border, with no padding between the arrow and the border. This looks cramped and inconsistent. The fix adds adequate right padding so the chevron has breathing room. Fixes GitHub issue #40.

## Domain Context

- Bounded context: Shared (global CSS / frontend-only)
- Key entities involved: none (UI styling fix)
- Ubiquitous language: no domain terms affected
- Key files:
  - `apps/chairly/src/tailwind.css` — global CSS entry file processed by PostCSS; the fix goes here as a global `select` rule

## Frontend Tasks

### F1 — Add right padding to select elements globally

Add a global CSS rule in `apps/chairly/src/tailwind.css` that gives native `<select>` elements sufficient right padding so the native browser chevron is visually separated from the right border.

**File:** `apps/chairly/src/tailwind.css`

Append the following rule after the existing dark-mode overrides (at the end of the file):

```css
/* Give native select dropdowns breathing room on the right for the browser chevron */
select {
  padding-right: 2rem;
}
```

This ensures that in all `<select>` elements across the app — regardless of which component they live in — the native arrow has space from the right edge. Since `<select>` elements already use `px-3` in their Tailwind classes, `padding-right: 2rem` overrides that specific side to provide more room.

**Verify** by checking the booking list page's staff filter dropdown, the booking form service/client/staff dropdowns, and the service form's category dropdown. In all cases the chevron should appear with a few pixels of space from the right border.

**Dark mode:** the existing dark-mode `select` override in `tailwind.css` (lines 89–95) sets background/color/border for `[data-theme='dark'] select`. Add `padding-right: 2rem` to that rule as well to ensure it is not overridden in dark mode:

```css
[data-theme='dark'] input,
[data-theme='dark'] textarea,
[data-theme='dark'] select {
  background-color: #1e293b;
  color: #f1f5f9;
  border-color: #475569;
  padding-right: 2rem;
}
```

Wait — if the global `select { padding-right: 2rem }` rule appears after the dark mode block, it will override it, so this additional line may not be needed. Check specificity: the dark-mode rule uses an attribute selector (`[data-theme='dark'] select`) which has higher specificity than a plain `select` rule. Therefore add `padding-right: 2rem` to the dark-mode `select` block as well, to ensure consistency.

## Acceptance Criteria

- [ ] `select { padding-right: 2rem; }` added to `tailwind.css`
- [ ] `padding-right: 2rem` also present in the `[data-theme='dark'] select` block
- [ ] The native dropdown chevron has visible space from the right border in both light and dark mode
- [ ] No regression in other form inputs (only `<select>` is affected)
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Replacing native `<select>` with a custom dropdown component using `appearance-none`
- Backend changes
- Changes to `<input>` or `<textarea>` padding
