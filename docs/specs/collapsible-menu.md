# Collapsible Menu

## Overview

The shell sidebar navigation is a fixed 240px (`w-60`) column that cannot be collapsed. This makes the app unusable on mobile devices and wastes screen space on small viewports. The sidebar needs a hamburger toggle button and responsive behavior: collapsed by default on mobile, expanded on desktop. Fixes GitHub issue #7.

## Domain Context

- Bounded context: Shared (shell component)
- Key files:
  - `libs/shared/src/lib/ui/shell/shell.component.ts`
  - `libs/shared/src/lib/ui/shell/shell.component.html`

## Frontend Tasks

### F1 — Make the sidebar collapsible with responsive behavior

Modify the `ShellComponent` to support a collapsible sidebar with a hamburger toggle.

**Behavior:**
- **Desktop (≥ 768px / `md`):** sidebar is expanded by default, can be toggled via a collapse button
- **Mobile (< 768px):** sidebar is hidden by default, opens as an overlay when the hamburger is clicked, closes when a nav link is clicked or the overlay backdrop is clicked
- Sidebar state is stored in a `signal<boolean>` (`sidebarOpen`)

**Template changes to `shell.component.html`:**

1. **Add a top bar (mobile only)** with a hamburger button:
   ```html
   <div class="md:hidden flex items-center justify-between bg-primary-700 px-4 py-3 text-white">
     <span class="text-lg font-bold">Chairly</span>
     <button type="button" (click)="toggleSidebar()" aria-label="Menu openen">
       <!-- Hamburger SVG icon (3 horizontal lines) -->
     </button>
   </div>
   ```

2. **Sidebar overlay (mobile):** when `sidebarOpen()` is true on mobile, show a backdrop:
   ```html
   @if (sidebarOpen()) {
     <div class="fixed inset-0 z-30 bg-black/50 md:hidden" (click)="closeSidebar()"></div>
   }
   ```

3. **Sidebar nav:** change from static `w-60` to responsive:
   - Mobile: `fixed inset-y-0 left-0 z-40 w-60 transform transition-transform` + `translate-x-0` when open, `-translate-x-full` when closed
   - Desktop: `relative w-60` (always visible, or collapsible to icons-only)
   - Apply `md:relative md:translate-x-0` to keep it static on desktop

4. **Close on navigation (mobile):** when a `routerLink` is clicked, call `closeSidebar()` so the overlay closes automatically after navigation

5. **Theme toggle and nav links stay as-is** — only the container layout changes

**Component changes to `shell.component.ts`:**
- Add `sidebarOpen = signal(false)` (default closed on mobile)
- Add `toggleSidebar()` method: `this.sidebarOpen.update(v => !v)`
- Add `closeSidebar()` method: `this.sidebarOpen.set(false)`
- Wire `closeSidebar()` to nav link clicks

**Accessibility:**
- Hamburger button: `aria-label="Menu openen"` / `aria-label="Menu sluiten"` based on state
- Use `aria-expanded` attribute on the hamburger button
- Trap focus inside sidebar when it's open as overlay (optional, nice-to-have)

### F2 — Update e2e tests for collapsible menu

Add or update Playwright e2e tests to verify the collapsible menu.

**File:** `apps/chairly-e2e/src/navigation.spec.ts` (new file)

**Scenarios:**
1. **Desktop: sidebar visible by default** — at desktop viewport, verify the nav sidebar and all links (Diensten, Klanten, Medewerkers) are visible
2. **Mobile: sidebar hidden by default** — set viewport to mobile (375×667), verify sidebar is not visible
3. **Mobile: hamburger opens sidebar** — set viewport to mobile, click hamburger, verify sidebar slides in and links are visible
4. **Mobile: clicking a link closes sidebar** — open sidebar, click a nav link, verify sidebar closes
5. **Mobile: clicking backdrop closes sidebar** — open sidebar, click the backdrop overlay, verify sidebar closes

Use `page.setViewportSize({ width: 375, height: 667 })` for mobile viewport.
Mock all API calls to prevent ECONNREFUSED (use the `fixtures.ts` pattern from e2e-infrastructure spec).

### F3 — Unit tests for ShellComponent

Add or update unit tests for the sidebar toggle behavior.

**File:** `libs/shared/src/lib/ui/shell/shell.component.spec.ts`

**Test cases:**
- `sidebarOpen` signal defaults to `false`
- `toggleSidebar()` flips the value
- `closeSidebar()` sets to `false`

## Acceptance Criteria

- [ ] Sidebar is collapsible on all viewports
- [ ] Mobile: sidebar hidden by default, hamburger button visible, overlay opens/closes correctly
- [ ] Desktop: sidebar visible by default
- [ ] Clicking a nav link on mobile closes the sidebar automatically
- [ ] Clicking the backdrop on mobile closes the sidebar
- [ ] Theme toggle still works in collapsed/expanded state
- [ ] Dark mode styling works correctly for hamburger bar and overlay
- [ ] All UI text is in Dutch (aria-labels, button titles)
- [ ] Playwright e2e tests cover mobile and desktop sidebar behavior
- [ ] All frontend quality checks pass (lint, test, build)

## Out of Scope

- Desktop: collapsing sidebar to icon-only rail (keep it simple — desktop sidebar stays full width)
- Bottom tab navigation for mobile (future mobile UX enhancement)
- Off-canvas animations with spring physics
