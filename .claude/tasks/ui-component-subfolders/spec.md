# UI Component Subfolders

## Overview

Presentational (dumb) components in `ui/` folders are placed as flat files (e.g. `ui/service-table.component.ts`) instead of being grouped in subfolders (e.g. `ui/service-table/service-table.component.ts`). This violates Angular best practices for component organization and makes it harder to add co-located files (specs, styles). All UI components must be moved into their own subfolders. Fixes GitHub issue #22.

## Domain Context

- Bounded context: All domains (Services, Staff, Clients) + Shared
- Key entities involved: No domain entities are modified -- this is a structural refactor of the frontend `ui/` layers
- Ubiquitous language: N/A (no domain logic changes)
- The CLAUDE.md convention already requires smart components in `feature/{feature-name}/` subfolders, but does not explicitly enforce the same for `ui/` components. This spec extends that convention.

**Current flat structure (example -- services):**
```
services/ui/
├── category-panel.component.ts
├── category-panel.component.html
├── category-panel.component.spec.ts
├── service-form-dialog.component.ts
├── service-form-dialog.component.html
├── service-form-dialog.component.spec.ts
├── service-table.component.ts
├── service-table.component.html
├── service-table.component.spec.ts
└── index.ts
```

**Target structure:**
```
services/ui/
├── category-panel/
│   ├── category-panel.component.ts
│   ├── category-panel.component.html
│   └── category-panel.component.spec.ts
├── service-form-dialog/
│   ├── service-form-dialog.component.ts
│   ├── service-form-dialog.component.html
│   └── service-form-dialog.component.spec.ts
├── service-table/
│   ├── service-table.component.ts
│   ├── service-table.component.html
│   └── service-table.component.spec.ts
└── index.ts
```

## Frontend Tasks

### F1 -- Move services UI components into subfolders

Move each component in `libs/chairly/src/lib/services/ui/` into its own subfolder.

**Components to move:**
- `category-panel.component.{ts,html,spec.ts}` -> `category-panel/`
- `service-form-dialog.component.{ts,html,spec.ts}` -> `service-form-dialog/`
- `service-table.component.{ts,html,spec.ts}` -> `service-table/`

**Steps:**
1. Create subdirectories: `category-panel/`, `service-form-dialog/`, `service-table/`
2. Move each component's `.ts`, `.html`, and `.spec.ts` files into the corresponding subfolder
3. Verify `templateUrl` in each `.ts` file uses `./` relative path (e.g. `templateUrl: './category-panel.component.html'`). Since the `.html` file is co-located after the move, this should already be correct.
4. Update `ui/index.ts` barrel to import from the new subfolder paths:
   ```typescript
   export { CategoryPanelComponent } from './category-panel/category-panel.component';
   export { ServiceFormDialogComponent } from './service-form-dialog/service-form-dialog.component';
   export { ServiceTableComponent } from './service-table/service-table.component';
   ```
5. Grep the entire frontend workspace for any direct imports from the old flat paths (e.g. `from '.*services/ui/category-panel.component'`). All consumers should be importing from the barrel (`index.ts`) or from `@org/chairly-lib`, so no changes should be needed -- but verify.
6. Run `npx nx affected -t test --base=main` to confirm all services-domain tests still pass.
7. Run `npx nx affected -t build --base=main` to confirm the build succeeds.

**Test verification:**
- Existing unit tests for `CategoryPanelComponent`, `ServiceFormDialogComponent`, and `ServiceTableComponent` must pass without modification (only file paths change, not code).

### F2 -- Move staff UI components into subfolders

Move each component in `libs/chairly/src/lib/staff/ui/` into its own subfolder.

**Components to move:**
- `staff-avatar.component.{ts,html,spec.ts}` -> `staff-avatar/`
- `staff-form-dialog.component.{ts,html,spec.ts}` -> `staff-form-dialog/`
- `staff-table.component.{ts,html,spec.ts}` -> `staff-table/`
- `shift-schedule-editor.component.{ts,html,spec.ts}` -> `shift-schedule-editor/`

**Steps:**
1. Create subdirectories: `staff-avatar/`, `staff-form-dialog/`, `staff-table/`, `shift-schedule-editor/`
2. Move each component's `.ts`, `.html`, and `.spec.ts` files into the corresponding subfolder
3. Verify `templateUrl` uses `./` relative path in each `.ts` file
4. Update `ui/index.ts` barrel to import from the new subfolder paths:
   ```typescript
   export { ShiftScheduleEditorComponent } from './shift-schedule-editor/shift-schedule-editor.component';
   export { StaffAvatarComponent } from './staff-avatar/staff-avatar.component';
   export { StaffFormDialogComponent } from './staff-form-dialog/staff-form-dialog.component';
   export { StaffTableComponent } from './staff-table/staff-table.component';
   ```
5. Grep the entire frontend workspace for any direct imports from the old flat paths. Fix if found.
6. Run tests and build to verify.

**Test verification:**
- Existing unit tests for all four staff UI components must pass without modification.

### F3 -- Move clients UI components into subfolders

Move each component in `libs/chairly/src/lib/clients/ui/` into its own subfolder.

**Components to move:**
- `client-form-dialog.component.{ts,html,spec.ts}` -> `client-form-dialog/`
- `client-table.component.{ts,html,spec.ts}` -> `client-table/`

**Steps:**
1. Create subdirectories: `client-form-dialog/`, `client-table/`
2. Move each component's `.ts`, `.html`, and `.spec.ts` files into the corresponding subfolder
3. Verify `templateUrl` uses `./` relative path in each `.ts` file
4. Update `ui/index.ts` barrel to import from the new subfolder paths:
   ```typescript
   export { ClientFormDialogComponent } from './client-form-dialog/client-form-dialog.component';
   export { ClientTableComponent } from './client-table/client-table.component';
   ```
5. Grep the entire frontend workspace for any direct imports from the old flat paths. Fix if found.
6. Run tests and build to verify.

**Test verification:**
- Existing unit tests for `ClientFormDialogComponent` and `ClientTableComponent` must pass without modification.

### F4 -- Move shared UI components into subfolders

Move flat components in `libs/shared/src/lib/ui/` into their own subfolders. The `shell/` component is already in a subfolder. The `ThemeService` is an `@Injectable` service (not a component) and stays at the `ui/` root level.

**Components to move:**
- `confirmation-dialog.component.{ts,html,spec.ts}` -> `confirmation-dialog/`

**Note:** `theme.service.ts` and `theme.service.spec.ts` are services, not components. They remain flat at the `ui/` level.

**Steps:**
1. Create subdirectory: `confirmation-dialog/`
2. Move `confirmation-dialog.component.ts`, `confirmation-dialog.component.html`, and `confirmation-dialog.component.spec.ts` into `confirmation-dialog/`
3. Verify `templateUrl` uses `./` relative path
4. Update `ui/index.ts` barrel:
   ```typescript
   export { ConfirmationDialogComponent } from './confirmation-dialog/confirmation-dialog.component';
   export { ShellComponent } from './shell/shell.component';
   export { ThemeService } from './theme.service';
   ```
5. Grep the entire frontend workspace for any direct imports from the old flat path. Fix if found.
6. Run tests and build to verify.

**Test verification:**
- Existing unit test for `ConfirmationDialogComponent` must pass without modification.

### F5 -- Update CLAUDE.md and frontend domain skill with subfolder convention

Add an explicit convention rule for UI component subfolders to both documentation files.

**In CLAUDE.md** (`/home/jreuvers/projects/Chairly/CLAUDE.md`):

Add a row to the "Domain folder conventions -- checklist" table:
```
| Presentational components | `ui/{component-name}/` subfolder | Placing files directly in `ui/` |
```

Add to the "Forbidden" section:
```
- No flat component files directly in `ui/` -- every presentational component must be in its own `ui/{component-name}/` subfolder
```

**In `.claude/skills/chairly-frontend-domain/SKILL.md`**:

Update the "Domain Folder Structure" tree to show the subfolder convention under `ui/`:
```
├── ui/
│   ├── {component-name}/
│   │   ├── {component-name}.component.ts
│   │   ├── {component-name}.component.html
│   │   └── {component-name}.component.spec.ts
│   └── index.ts                   <- barrel: export all presentational components
```

Add a row to the "Checklist Before Creating Files" table:
```
| Presentational components | `ui/{component-name}/` subfolder |
```

**No tests needed for this task** -- it is documentation only.

## Acceptance Criteria

- [ ] All UI components in `services/ui/` are in their own subfolders (F1)
- [ ] All UI components in `staff/ui/` are in their own subfolders (F2)
- [ ] All UI components in `clients/ui/` are in their own subfolders (F3)
- [ ] All UI components in `shared/ui/` are in their own subfolders (F4)
- [ ] All barrel files (`index.ts`) export from the correct new subfolder paths
- [ ] No broken imports anywhere in the codebase
- [ ] CLAUDE.md updated with the subfolder convention for UI components (F5)
- [ ] Frontend domain skill updated with the subfolder convention (F5)
- [ ] All frontend quality checks pass: `npx nx affected -t lint --base=main`
- [ ] All frontend tests pass: `npx nx affected -t test --base=main`
- [ ] All frontend builds succeed: `npx nx affected -t build --base=main`
- [ ] All existing e2e tests still pass

## Out of Scope

- Adding ESLint rules to enforce subfolder grouping at CI (nice-to-have, but no existing plugin supports this well)
- Moving feature components (they are already in subfolders)
- Moving services (e.g. `ThemeService`) into subfolders -- services are not components and do not need subfolders
- Changing any component logic, templates, or test assertions
