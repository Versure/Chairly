# UI Component Subfolders

## Overview

Presentational (dumb) components in `ui/` folders are placed as flat files (e.g. `ui/service-table.component.ts`) instead of being grouped in subfolders (e.g. `ui/service-table/service-table.component.ts`). This violates Angular best practices for component organization and makes it harder to add co-located files (specs, styles). All UI components must be moved into their own subfolders. Fixes GitHub issue #22.

## Domain Context

- Bounded context: All domains (Services, Staff, Clients) + Shared
- The CLAUDE.md convention already requires smart components in `feature/{feature-name}/` subfolders, but does not explicitly enforce the same for `ui/` components. This spec extends that convention.

**Current flat structure (example — services):**
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

### F1 — Move services UI components into subfolders

Move each component in `libs/chairly/src/lib/services/ui/` into its own subfolder.

**Components to move:**
- `category-panel.component.{ts,html,spec.ts}` → `category-panel/`
- `service-form-dialog.component.{ts,html,spec.ts}` → `service-form-dialog/`
- `service-table.component.{ts,html,spec.ts}` → `service-table/`

**After moving:**
1. Update `templateUrl` in each `.ts` file to use `./` relative path (it should already work if the `.html` is co-located)
2. Update `ui/index.ts` barrel to import from the new subfolder paths:
   ```typescript
   export { CategoryPanelComponent } from './category-panel/category-panel.component';
   export { ServiceFormDialogComponent } from './service-form-dialog/service-form-dialog.component';
   export { ServiceTableComponent } from './service-table/service-table.component';
   ```
3. Verify no other files import directly from the old flat paths (use grep)

### F2 — Move staff UI components into subfolders

Move each component in `libs/chairly/src/lib/staff/ui/` into its own subfolder.

**Components to move:**
- `staff-avatar.component.{ts,html,spec.ts}` → `staff-avatar/`
- `staff-form-dialog.component.{ts,html,spec.ts}` → `staff-form-dialog/`
- `staff-table.component.{ts,html,spec.ts}` → `staff-table/`
- `shift-schedule-editor.component.{ts,html,spec.ts}` → `shift-schedule-editor/`

**After moving:** same barrel update pattern as F1.

### F3 — Move clients UI components into subfolders

Move each component in `libs/chairly/src/lib/clients/ui/` into its own subfolder.

**Components to move:**
- `client-form-dialog.component.{ts,html,spec.ts}` → `client-form-dialog/`
- `client-table.component.{ts,html,spec.ts}` → `client-table/`

**After moving:** same barrel update pattern as F1.

### F4 — Move shared UI components into subfolders (if flat)

Check `libs/shared/src/lib/ui/` for any flat components and move them into subfolders. The shell component is already in a subfolder (`shell/`). Check for:
- `confirmation-dialog.component.{ts,html,spec.ts}` — if flat, move to `confirmation-dialog/`
- Any other flat components

Update `libs/shared/src/lib/ui/index.ts` barrel accordingly.

### F5 — Update CLAUDE.md and frontend domain skill with subfolder convention

Add an explicit rule to CLAUDE.md and the frontend domain skill:

**In CLAUDE.md** (Domain folder conventions table), add:
| Presentational components | `ui/{component-name}/` subfolder | Placing files directly in `ui/` |

**In `.claude/skills/chairly-frontend-domain/SKILL.md`** (Domain Folder Structure), update the `ui/` section:
```
├── ui/
│   ├── {component-name}/
│   │   ├── {component-name}.component.ts
│   │   ├── {component-name}.component.html
│   │   └── {component-name}.component.spec.ts
│   └── index.ts
```

**In Checklist Before Creating Files table**, add:
| Presentational components | `ui/{component-name}/` subfolder |

## Acceptance Criteria

- [ ] All UI components across all domains are in their own subfolders
- [ ] All barrel files (`index.ts`) export from the correct new paths
- [ ] No broken imports anywhere in the codebase
- [ ] CLAUDE.md updated with the subfolder convention for UI components
- [ ] Frontend domain skill updated with the subfolder convention
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] All existing e2e tests still pass

## Out of Scope

- Adding ESLint rules to enforce subfolder grouping at CI (nice-to-have, but no existing plugin supports this well)
- Moving feature components (they're already in subfolders)
