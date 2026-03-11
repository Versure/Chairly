# Navigation Icons

## Overview

The main sidebar navigation has icons on the "Instellingen" item and the light/dark mode toggle, but all other nav items (Boekingen, Diensten, Facturen, Klanten, Medewerkers) lack icons. This inconsistency makes the menu look unfinished. All nav items should have a consistent icon prefix. Fixes GitHub issue #48.

---

## Domain Context

- **Bounded context:** Shared (shell/navigation)
- **Key files involved:**
  - `libs/shared/src/lib/ui/shell/shell.component.html`
  - `libs/shared/src/lib/ui/shell/shell.component.ts`

---

## Frontend Tasks

### F1 — Add icons to all nav items in the sidebar

**File:** `libs/shared/src/lib/ui/shell/shell.component.html`

Add an SVG icon to each nav item that currently lacks one. Use inline SVG icons from the [Heroicons](https://heroicons.com/) set (outline style, 20×20) to stay consistent with the existing settings icon.

Suggested icon mapping:

| Nav item | Heroicons name | Description |
|---|---|---|
| Boekingen | `calendar-days` | Calendar with day grid |
| Diensten | `scissors` / `wrench-screwdriver` | Service/tool icon |
| Facturen | `document-text` | Document with lines |
| Klanten | `users` | Two people silhouette |
| Medewerkers | `user-group` | Group of people |

Each nav item `<a>` element should follow this pattern (matching the existing Instellingen icon pattern):

```html
<a routerLink="/boekingen" routerLinkActive="bg-primary-600 text-white"
   class="flex items-center gap-3 rounded px-3 py-2 text-sm font-medium ..."
   (click)="closeSidebar()">
  <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
    <!-- path data -->
  </svg>
  Boekingen
</a>
```

All icons must:
- Use `class="h-5 w-5 shrink-0"` for consistent sizing
- Use `aria-hidden="true"` (decorative — text label provides the accessible name)
- Use outline/stroke style (not filled) for visual consistency with the existing settings icon

### F2 — Verify active state still works

Ensure the `routerLinkActive` class (`bg-primary-600 text-white`) is correctly applied and that icon color inherits from the text color (use `stroke="currentColor"`) so icons automatically turn white when the item is active.

---

## Acceptance Criteria

- [ ] All 5 nav items (Boekingen, Diensten, Facturen, Klanten, Medewerkers) have an icon prefix
- [ ] Icons use the same style (outline, 20×20 Heroicons) as the existing Instellingen icon
- [ ] Icons inherit text color via `currentColor` so they display correctly in active (white) and inactive states
- [ ] Icons are `aria-hidden="true"` (decorative)
- [ ] No layout regressions — existing spacing and active state styling unchanged
- [ ] Works correctly in both light and dark mode
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Existing e2e tests still pass

---

## Out of Scope

- Adding icons to mobile/hamburger menu toggle button (already has a hamburger icon)
- Icon-only collapsed sidebar mode
- Animated icons or hover effects
