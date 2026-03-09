# Collapsible Menu

## Overview

The shell sidebar navigation is a fixed 240px (`w-60`) column that cannot be collapsed. This makes the app unusable on mobile devices and wastes screen space on small viewports. The sidebar needs a hamburger toggle button and responsive behavior: collapsed by default on mobile, expanded on desktop. Fixes GitHub issue #7.

## Domain Context

- Bounded context: Shared (shell component)
- Key entities involved: None (UI-only feature, no domain entities)
- Ubiquitous language: N/A (this is a shell layout concern, not a domain feature)
- Key files:
  - `libs/shared/src/lib/ui/shell/shell.component.ts`
  - `libs/shared/src/lib/ui/shell/shell.component.html`

## Frontend Tasks

### F1 — Make the sidebar collapsible with responsive behavior

Modify the `ShellComponent` to support a collapsible sidebar with a hamburger toggle.

**Behavior:**
- **Desktop (>= 768px / `md`):** sidebar is expanded by default, always visible as a static column. No collapse/expand toggle needed on desktop.
- **Mobile (< 768px):** sidebar is hidden by default, opens as an overlay when the hamburger button is clicked, closes when a nav link is clicked or the overlay backdrop is clicked.
- Sidebar state is stored in a `signal<boolean>` (`sidebarOpen`), defaulting to `false`.

**Template changes to `shell.component.html`:**

1. **Add a top bar (mobile only)** with a hamburger button:
   ```html
   <div class="md:hidden flex items-center justify-between bg-primary-700 px-4 py-3 text-white">
     <span class="text-lg font-bold">Chairly</span>
     <button
       type="button"
       (click)="toggleSidebar()"
       [attr.aria-label]="sidebarOpen() ? 'Menu sluiten' : 'Menu openen'"
       [attr.aria-expanded]="sidebarOpen()">
       <!-- Hamburger SVG icon (3 horizontal lines) -->
       <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
         <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
       </svg>
     </button>
   </div>
   ```

2. **Sidebar overlay backdrop (mobile):** when `sidebarOpen()` is true on mobile, show a backdrop:
   ```html
   @if (sidebarOpen()) {
     <div class="fixed inset-0 z-30 bg-black/50 md:hidden" (click)="closeSidebar()"></div>
   }
   ```

3. **Sidebar nav:** change from static `w-60` to responsive classes:
   - Mobile: `fixed inset-y-0 left-0 z-40 w-60 transform transition-transform` + `translate-x-0` when open, `-translate-x-full` when closed
   - Desktop: `md:relative md:translate-x-0` to keep it as a static sidebar
   - The sidebar retains all existing content (logo header, nav links, theme toggle)

4. **Close on navigation (mobile):** add `(click)="closeSidebar()"` to each `routerLink` anchor so the overlay closes after navigation on mobile.

5. **Theme toggle and nav links stay as-is** -- only the container layout changes.

**Component changes to `shell.component.ts`:**
- Add `sidebarOpen = signal(false)` (default closed, relevant for mobile)
- Add `toggleSidebar(): void` method: `this.sidebarOpen.update(v => !v)`
- Add `closeSidebar(): void` method: `this.sidebarOpen.set(false)`
- Wire `closeSidebar()` to nav link clicks in the template

**Accessibility:**
- Hamburger button: `aria-label="Menu openen"` / `aria-label="Menu sluiten"` based on `sidebarOpen()` state
- Use `[attr.aria-expanded]="sidebarOpen()"` on the hamburger button
- All aria-label text is in Dutch

**Dark mode considerations:**
- The mobile top bar uses `bg-primary-700` which is a brand color. Verify it looks correct in dark mode. If needed, add a `dark:` variant (e.g. `dark:bg-slate-800`).
- The overlay backdrop (`bg-black/50`) works in both light and dark modes.

**Existing code reference -- current `shell.component.ts`:**
```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from '../theme.service';

@Component({
  selector: 'chairly-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
})
export class ShellComponent {
  protected readonly themeService = inject(ThemeService);
}
```

The signal import must be added: `import { ..., signal } from '@angular/core';`

**Existing code reference -- current `shell.component.html`:**
The current template is a simple `flex h-screen` layout with a static `w-60` nav and a `flex-1` main area with `<router-outlet />`. The nav contains:
- Logo header ("Chairly")
- Navigation links: Diensten (`/diensten`), Klanten (`/klanten`), Medewerkers (`/medewerkers`)
- Theme toggle button at the bottom

### F2 — Update e2e tests for collapsible menu

Add Playwright e2e tests to verify the collapsible menu behavior at mobile and desktop viewports.

**File:** `apps/chairly-e2e/src/navigation.spec.ts` (new file)

**Import pattern:** Use the shared fixtures from `./fixtures` (not `@playwright/test` directly):
```typescript
import { expect, test } from './fixtures';
```

**No API mocks needed beyond the global fallback** -- the navigation test does not load any domain data, so the global `/api/**` fallback from `fixtures.ts` is sufficient. However, if the shell triggers any API calls on load (check the app initialization), mock them accordingly.

**Scenarios:**

1. **Desktop: sidebar visible by default**
   - Use default viewport (desktop-sized, typically 1280x720)
   - Navigate to `/`
   - Verify the sidebar nav is visible
   - Verify all nav links are visible: "Diensten", "Klanten", "Medewerkers"
   - Verify the hamburger button is NOT visible (hidden via `md:hidden`)

2. **Mobile: sidebar hidden by default**
   - Set viewport to mobile: `page.setViewportSize({ width: 375, height: 667 })`
   - Navigate to `/`
   - Verify the sidebar nav is NOT visible (translated off-screen)
   - Verify the hamburger button IS visible

3. **Mobile: hamburger opens sidebar**
   - Set viewport to mobile
   - Navigate to `/`
   - Click the hamburger button
   - Verify the sidebar slides in and nav links ("Diensten", "Klanten", "Medewerkers") are visible
   - Verify the overlay backdrop is present

4. **Mobile: clicking a nav link closes sidebar**
   - Set viewport to mobile
   - Navigate to `/`, open the sidebar via hamburger
   - Click a nav link (e.g. "Diensten")
   - Verify the sidebar is no longer visible (closed)

5. **Mobile: clicking backdrop closes sidebar**
   - Set viewport to mobile
   - Navigate to `/`, open the sidebar via hamburger
   - Click the backdrop overlay element
   - Verify the sidebar is no longer visible (closed)

### F3 — Unit tests for ShellComponent

Add unit tests for the sidebar toggle behavior in the ShellComponent.

**File:** `libs/shared/src/lib/ui/shell/shell.component.spec.ts` (new file)

**Test setup:**
- Use Angular `TestBed` with `provideRouter([])` for the RouterLink/RouterOutlet dependencies
- The component uses `ThemeService` -- provide it or a mock

**Test cases:**

1. **`sidebarOpen` signal defaults to `false`**
   - Create the component
   - Assert `component.sidebarOpen()` is `false`

2. **`toggleSidebar()` flips the value**
   - Create the component
   - Call `component.toggleSidebar()`
   - Assert `component.sidebarOpen()` is `true`
   - Call `component.toggleSidebar()` again
   - Assert `component.sidebarOpen()` is `false`

3. **`closeSidebar()` sets to `false`**
   - Create the component
   - Set sidebar open: `component.toggleSidebar()` (now true)
   - Call `component.closeSidebar()`
   - Assert `component.sidebarOpen()` is `false`

4. **`closeSidebar()` is idempotent when already closed**
   - Create the component (sidebarOpen is false)
   - Call `component.closeSidebar()`
   - Assert `component.sidebarOpen()` is still `false`

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
- [ ] Unit tests cover signal state management (toggle, close)
- [ ] All frontend quality checks pass (lint, test, build)

## Out of Scope

- Desktop: collapsing sidebar to icon-only rail (keep it simple -- desktop sidebar stays full width)
- Bottom tab navigation for mobile (future mobile UX enhancement)
- Off-canvas animations with spring physics
- Persisting sidebar state across page reloads (e.g. localStorage)
- Keyboard trap / focus management when sidebar overlay is open (nice-to-have, not required)
