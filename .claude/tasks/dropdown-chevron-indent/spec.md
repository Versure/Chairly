# Dropdown Chevron Indent

## Overview

Native `<select>` dropdown elements in the app have their browser-rendered chevron/arrow flush against the right border, with no padding between the arrow and the border. This looks cramped and inconsistent. The fix adds adequate right padding so the chevron has breathing room. Fixes GitHub issue #40.

This is a frontend-only styling fix in the Shared (global CSS) bounded context. No backend changes are required.

## Domain Context

- Bounded context: Shared (global CSS / frontend-only)
- Key entities involved: none (UI styling fix)
- Ubiquitous language: no domain terms affected
- Key files:
  - `src/frontend/chairly/apps/chairly/src/tailwind.css` -- global CSS entry file processed by PostCSS; the fix goes here as a global `select` rule

## Frontend Tasks

### F1 -- Add right padding to select elements globally

Add a global CSS rule in `src/frontend/chairly/apps/chairly/src/tailwind.css` that gives native `<select>` elements sufficient right padding so the native browser chevron is visually separated from the right border.

**File:** `src/frontend/chairly/apps/chairly/src/tailwind.css`

**Step 1 -- Add a global `select` padding rule:**

Append the following rule after the existing dark-mode overrides (at the end of the file, after the `hover:bg-gray-50` override block):

```css
/* Give native select dropdowns breathing room on the right for the browser chevron */
select {
  padding-right: 2rem;
}
```

This ensures that in all `<select>` elements across the app -- regardless of which component they live in -- the native arrow has space from the right edge. Since `<select>` elements already use `px-3` in their Tailwind classes, `padding-right: 2rem` overrides that specific side to provide more room.

**Step 2 -- Add `padding-right: 2rem` to the dark-mode form input override:**

The existing dark-mode rule for form inputs (lines 89-95 in the current file) uses an attribute selector `[data-theme='dark'] select` which has higher specificity than the plain `select` rule. To ensure the padding also applies in dark mode, add `padding-right: 2rem` to the existing dark-mode rule block:

```css
[data-theme='dark'] input,
[data-theme='dark'] textarea,
[data-theme='dark'] select {
  background-color: #1e293b; /* slate-800 */
  color: #f1f5f9; /* slate-100 */
  border-color: #475569; /* slate-600 */
  padding-right: 2rem;
}
```

**Important specificity note:** The dark-mode rule `[data-theme='dark'] select` has higher specificity (0,1,1) than the plain `select` rule (0,0,1). If the `padding-right` were only in the plain `select` rule, it would be overridden by the dark-mode block (which currently does not set `padding-right`, so the dark-mode block would implicitly not apply any right padding override). Adding `padding-right: 2rem` to both rules ensures consistent behavior in light and dark mode.

**However**, note that CSS does not reset properties -- the dark-mode rule only sets `background-color`, `color`, and `border-color`, so it does not actually override `padding-right`. The plain `select` rule's `padding-right` would still apply in dark mode. Nonetheless, adding it to the dark-mode block as well is a defensive measure and makes the intent explicit.

**Verification:**

- Check the booking list page's staff filter dropdown
- Check the booking form service/client/staff dropdowns
- Check the service form's category dropdown
- In all cases the chevron should appear with visible space from the right border in both light and dark mode
- Confirm no regression in `<input>` or `<textarea>` elements (only `<select>` is affected)

**Quality checks to run:**

```bash
cd src/frontend/chairly
npx nx affected -t lint --base=main
npx nx format:check --base=main
npx nx affected -t test --base=main
npx nx affected -t build --base=main
```

## Acceptance Criteria

- [ ] `select { padding-right: 2rem; }` added to `tailwind.css` after the dark-mode overrides
- [ ] `padding-right: 2rem` also present in the `[data-theme='dark'] select` block (lines 89-95)
- [ ] The native dropdown chevron has visible space from the right border in both light and dark mode
- [ ] No regression in other form inputs (only `<select>` is affected)
- [ ] All frontend quality checks pass (`npx nx affected -t lint --base=main`, `npx nx format:check --base=main`, `npx nx affected -t test --base=main`, `npx nx affected -t build --base=main`)
- [ ] Existing e2e tests still pass

## Out of Scope

- Replacing native `<select>` with a custom dropdown component using `appearance-none`
- Backend changes
- Changes to `<input>` or `<textarea>` padding
