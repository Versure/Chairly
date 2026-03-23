# Subscription Management Portal — Implementation Log

Feature: **subscription-management-portal** — Internal admin portal for Chairly SaaS owner to manage all tenant subscriptions
Date: 2026-03-22
Spec PR: https://github.com/Versure/Chairly/pull/83
Implementation PR: https://github.com/Versure/Chairly/pull/84

---

## Part 1: Spec Creation

### Step 0 — Parse arguments

- **Feature name:** `subscription-management-portal`
- **Spec branch:** `spec/subscription-management-portal`
- **No GitHub issue** provided — feature described interactively

### Step 1 — Gather context

Feature description was terse ("subscription-management-portal"). The spec-writer agent presented 3 initial scope decisions.

**User clarification:** This is NOT a tenant-facing portal. The user is the Chairly platform owner and wants an internal admin portal to manage all subscriptions and monitor/service all tenant instances. It should be an entirely separate application, not part of the customer-facing Chairly app. Tenant self-service subscription management will come later through the Chairly website.

### Step 2 — Write spec (interactive decisions)

The spec-writer agent presented 8 decisions across 2 rounds:

| # | Decision | Options | Choice |
|---|----------|---------|--------|
| 1 | Scope | Tenant self-service / Admin dashboard / Both | **Custom: Platform owner admin portal** — manage all subscriptions + monitor tenant instances |
| 2 | Bounded context | Keep in Onboarding / New Subscriptions context / Add to Settings | **B: New bounded context** — platform-level, not tenant-scoped |
| 3 | Frontend location | Settings section / Settings child route / New app | **Custom: Entirely new application** — separate from customer-facing Chairly |
| 4 | Backend API structure | New Features/Admin/ area / Separate AdminApi project / Same API + MapGroup | **A: New `Features/Admin/` area** in existing Chairly.Api |
| 5 | Authentication | New realm role / Separate Keycloak realm / Separate client | **B: Separate `chairly-admin` Keycloak realm** — strict risk management, realm isolation |
| 6 | Frontend app naming | `apps/chairly-admin/` / `apps/admin-portal/` | **A: `apps/chairly-admin/`** with `libs/admin/` domain library |
| 7 | MVP feature scope | Subscriptions only / + tenant overview / Full ops portal | **A: Subscriptions management only** for MVP |
| 8 | Update capabilities | Provision+Cancel only / + plan/billing update / Full lifecycle | **B: Provision, Cancel, and Update plan/billing cycle** |

**Key architectural decisions captured:**
- Dedicated Keycloak realm `chairly-admin` for complete auth isolation
- `platform_admin` role bypasses tenant context middleware
- Admin endpoints under `/api/admin/subscriptions/` with `RequirePlatformAdmin` policy
- Admin shell component at `libs/admin/src/lib/layout/` (cross-domain, not inside subscriptions domain)
- Sheriff tags: `admin-lib`, `admin-layout`, `domain:subscriptions-admin` (avoids conflict with future tenant-facing subscriptions)

### Step 3 — Review spec

**First review** found **6 issues:**

1. All 16 headings used `--` instead of `—` (em-dash)
2. AdminShellComponent placement left unresolved ("implementer should evaluate")
3. Provision dialog inconsistency — inline vs extracted component
4. Missing B6 test case for invalid BillingCycle
5. `[Required]` on Guid route params is a no-op
6. F4 URL query param sync mechanism underspecified

All 6 findings fixed by re-spawning the spec-writer agent.

**Second review** found **4 more issues:**

1. Task IDs `F6a`/`F6b` non-standard — renumbered to sequential F6-F9
2. B2 missing `[Range]` Data Annotations on Page/PageSize
3. Dialog outputs used `new OutputEmitterRef<void>()` instead of `output<void>()`
4. F5 missing `DestroyRef` injection

All 4 findings fixed in a final spec-writer pass.

### Step 4 — Create PR

- Spec PR: https://github.com/Versure/Chairly/pull/83
- Merged to main before implementation

---

## Part 2: Implementation

### Phase 0 — Setup

- Pulled merged spec PR #83 to main
- Created feature branch: `feat/subscription-management-portal`
- Created worktrees:
  - `.worktrees/subscription-management-portal/backend/` on `impl/subscription-management-portal-backend`
  - `.worktrees/subscription-management-portal/frontend/` on `impl/subscription-management-portal-frontend`

### Phase 0.5 — Infrastructure Implementation

Infra tasks I1 and I2 detected. I1 (Keycloak seeder) handled by `infra-impl` agent in backend worktree. I2 (Angular scaffolding) split: backend config parts by `infra-impl`, frontend scaffolding by `frontend-impl` in Phase 1.

**Agent:** `infra-impl`

**I1 — Keycloak admin realm dev seeder:**
- Updated: `KeycloakDevSeeder.cs` — added `chairly-admin` realm, `chairly-admin-portal` client, `platform_admin` role, `admin@chairly.local` user
- Updated: `TenantContextMiddleware.cs` — `platform_admin` role bypass to skip tenant resolution
- Updated: `Program.cs` — `RequirePlatformAdmin` authorization policy, admin client ID added to JWT valid audiences
- Updated: `AppHost/Program.cs` — admin realm environment variables

**I2 — Backend config parts:**
- Updated: `appsettings.json` / `appsettings.Development.json` — Keycloak admin portal config
- Updated: `tsconfig.base.json` — `@org/admin-lib` path alias

**Quality gate:** build ✅, tests (372 passed) ✅, format ✅

**Infra review:** Reviewer flagged missing `Features/Admin/` and `GetAdminConfigEndpoint` — these are B1 backend tasks, not infra tasks. Infra implementation confirmed correct.

### Phase 1 — Backend + Frontend Implementation (parallel)

Both agents ran in parallel.

#### Backend Implementation

**Agent:** `backend-impl`
**Tasks:** B1, B2, B3, B4, B5, B6

**B1 — Admin endpoints infrastructure and GetAdminConfig:**
- Created: `Features/Admin/AdminEndpoints.cs` — route group with `RequirePlatformAdmin` policy
- Created: `Features/Config/GetAdminConfig/` — `GetAdminConfigQuery.cs`, `GetAdminConfigHandler.cs`, `GetAdminConfigEndpoint.cs` (`GET /api/config/admin`, AllowAnonymous)
- Created: `Features/Config/AdminConfigResponse.cs`
- Updated: `ConfigEndpoints.cs`, `Program.cs` — endpoint registration

**B2 — GetAdminSubscriptionsList:**
- Created: `Features/Admin/GetAdminSubscriptionsList/` — query, handler, endpoint (`GET /api/admin/subscriptions`)
- Paginated with search, status filter, plan filter
- Used `StringComparison.OrdinalIgnoreCase` for search (supports InMemory + Npgsql)

**B3 — GetAdminSubscription:**
- Created: `Features/Admin/GetAdminSubscription/` — query, handler, endpoint (`GET /api/admin/subscriptions/{id}`)

**B4 — ProvisionSubscription:**
- Created: `Features/Admin/ProvisionSubscription/` — command, handler, endpoint (`POST /api/admin/subscriptions/{id}/provision`)

**B5 — CancelSubscription:**
- Created: `Features/Admin/CancelSubscription/` — command, handler, endpoint (`POST /api/admin/subscriptions/{id}/cancel`)

**B6 — UpdateSubscriptionPlan:**
- Created: `Features/Admin/UpdateSubscriptionPlan/` — command, handler, endpoint (`PUT /api/admin/subscriptions/{id}/plan`)

**Shared:**
- Created: `AdminSubscriptionMapper.cs` — status derivation and response mapping
- Created: `AdminSubscriptionDetailResponse.cs`, `AdminSubscriptionsListResponse.cs`, `AdminSubscriptionListItem.cs`

**Tests:** `AdminSubscriptionHandlerTests.cs` — 28 new tests covering all 6 handlers

**Quality gate:** build ✅, tests ✅, format ✅

#### Frontend Implementation

**Agent:** `frontend-impl`
**Tasks:** I2 (frontend parts), F1, F2, F3, F4, F5, F6, F7, F8, F9

**I2 — Angular application scaffolding:**
- Created: `apps/chairly-admin/` — full Angular app (project.json, tsconfig files, eslint, proxy, vite, app component, config with Keycloak auth, routes, styles, index.html)
- Created: `libs/admin/` — domain library (project.json, tsconfig files, eslint, vite, barrel exports)
- Created: `apps/chairly-admin-e2e/` — Playwright e2e project with auth fixtures
- Updated: `sheriff.config.ts` — `admin-lib`, `admin-layout`, `domain:subscriptions-admin` tags and dep rules

**F1 — Models and API service:**
- Created: `subscriptions/models/subscription.model.ts` — TypeScript interfaces matching backend responses
- Created: `subscriptions/data-access/admin-subscription-api.service.ts` — API service
- Created: `subscriptions/data-access/admin-subscription.store.ts` — NgRx SignalStore

**F2 — Subscription status badge pipe:**
- Created: `subscriptions/pipes/subscription-status-badge.pipe.ts`
- Created: `subscriptions/pipes/billing-cycle.pipe.ts` (bonus: billing cycle formatting)

**F3 — Admin shell component:**
- Created: `layout/admin-shell/` — shell with nav sidebar and router-outlet

**F4 — Subscription list page:**
- Created: `feature/subscription-list-page/` — search, status/plan filters, pagination, URL query param sync

**F5 — Subscription detail page:**
- Created: `feature/subscription-detail-page/` — subscription info, status badge, action buttons for dialogs

**F6 — Provision subscription dialog:**
- Created: `ui/provision-subscription-dialog/` — confirmation dialog with salon name

**F7 — Cancel subscription dialog:**
- Created: `ui/cancel-subscription-dialog/` — dialog with reason textarea (required)

**F8 — Update plan dialog:**
- Created: `ui/update-plan-dialog/` — dialog with plan and billing cycle selects

**F9 — E2E tests:**
- Created: `subscription-list.spec.ts`, `subscription-detail.spec.ts`

**Quality gate:** lint ✅, format ✅, tests ✅, build ✅

### Phase 2 — Code Review (parallel)

#### Backend Review — 4 findings

1. `Unprocessable` vs spec's `ValidationFailed` naming — kept `Unprocessable` (project convention)
2. Missing test for `Page = -1 returns 422`
3. Migration `Down()` uses bare `CreateTable`/`CreateIndex` — pre-existing, not introduced by this feature
4. Redundant null check condition in `UpdateSubscriptionPlanHandler`

#### Frontend Review — 9 findings

1. Missing `queryParamsHandling: 'merge'` in `router.navigate()`
2. Dialog outputs named `cancelled` instead of spec's `cancel`
3. Missing unit tests for 5 components (list page, detail page, 3 dialogs)
4. Missing e2e scenarios for list page (search, filters, pagination, navigation)
5. Missing e2e confirm flows for detail page (provision, cancel, update plan)

#### Fix Pass

**Backend fixes applied:**
- Added `Page = -1` validation test
- Simplified redundant `billingCycle` null check in `UpdateSubscriptionPlanHandler`

**Frontend fixes applied:**
- Added `queryParamsHandling: 'merge'` to `router.navigate()`
- `cancelled` output name kept — `cancel` is a native DOM event blocked by `@angular-eslint/no-output-native`
- Created unit tests for all 5 components (31 tests total)
- Added 6 e2e scenarios to `subscription-list.spec.ts`
- Added 3 e2e confirm flow tests to `subscription-detail.spec.ts`

**Re-review results:**
- Backend: pass
- Frontend: 1 minor finding (unused `Router` injection in detail page) — deferred to QA lint

### Phase 3 — Quality Checks (parallel)

| Check | Backend | Frontend |
|-------|---------|----------|
| Build | ✅ pass | ✅ pass |
| Tests | ✅ 410 passed, 6 skipped | ✅ 40 passed |
| Format | ✅ pass | ✅ pass |
| Lint | N/A | ✅ pass (1 warning) |
| E2E | N/A | ✅ 56/56 (Chromium + Firefox) |

**Note:** WebKit e2e tests fail to launch due to missing system libraries (`libgtk-4.so.1`) in WSL2 — infrastructure issue, not code.

### Phase 4 — Merge and PR

- Committed all changes in both worktrees
- Pushed `impl/subscription-management-portal-backend` and `impl/subscription-management-portal-frontend`
- Merged both into `feat/subscription-management-portal` with `--no-ff`
- Resolved merge conflict in `tsconfig.base.json` (both worktrees added `@org/admin-lib` alias)
- Created PR: https://github.com/Versure/Chairly/pull/84

---

## Overall Summary

| Workflow | Notes |
|----------|-------|
| Spec creation (interactive) | 8 user decisions, 10 review findings fixed across 2 review passes |
| Implementation | 2 infra + 6 backend + 9 frontend tasks, parallel execution |
| Code review | Backend 2 findings fixed, frontend 5 findings fixed |
| QA checks | All gates green |

### What was delivered

- **New backend:** 6 admin API endpoints (`GET /api/config/admin`, `GET /api/admin/subscriptions`, `GET /api/admin/subscriptions/{id}`, `POST .../provision`, `POST .../cancel`, `PUT .../plan`) protected by `RequirePlatformAdmin` policy
- **New frontend app:** `chairly-admin` Angular app with Keycloak auth (`chairly-admin` realm), subscription list page with search/filter/pagination/URL sync, subscription detail page with action dialogs
- **Infrastructure:** Keycloak `chairly-admin` realm with `platform_admin` role, `TenantContextMiddleware` bypass for admin requests, Aspire AppHost config
- **Components:** Admin shell, subscription list page, subscription detail page, provision/cancel/update-plan dialog components, status badge pipe, billing cycle pipe
- **Tests:** 28 backend handler tests, 40 frontend unit tests, 56 Playwright e2e tests (Chromium + Firefox)
- **All UI in Dutch** — "Abonnementen", "Activeren", "Annuleren", "Abonnement bijwerken", etc.

---

## Part 3: PR Rework Passes

### Rework Pass 1 (2026-03-23) — Keycloak redirect fix

**PR comment:** Navigating to admin site redirects to Keycloak → "Page not found"

**Root cause:** In `KeycloakDevSeeder.SeedAsync`, `SeedAdminRealmAsync` (Step 5) was placed after an early return on line 58-62. When the tenant user already exists (every restart with persisted Keycloak data), the method returns before the admin realm is seeded.

**Fix:** Moved `SeedAdminRealmAsync` call before the tenant user existence check so it always runs (idempotent internally).

**Agent:** `infra-impl` (1 spawn) + `chairly-backend-qa` (1 spawn)

### Rework Pass 2 (2026-03-23) — Search crash + GUID display

**PR comments:**
1. Search returns 500 — `string.Contains(OrdinalIgnoreCase)` can't be translated by EF Core Npgsql
2. Subscription history shows raw GUIDs instead of user names for "activated by"/"cancelled by"

**Fixes:**
- **Search:** Replaced `string.Contains(OrdinalIgnoreCase)` with `ToLower().Contains()` (translates to `lower()` in PostgreSQL)
- **User names:** Added `GetUserDisplayNameAsync` to `IKeycloakAdminService` to resolve Keycloak user IDs to "FirstName LastName". Changed `AdminSubscriptionDetailResponse` By fields from `Guid?` to `string?` (`CreatedByName`, `ProvisionedByName`, `CancelledByName`). Updated all 4 admin handlers, mapper, tests. Frontend model/templates/specs updated to match renamed fields.

**Agents:** `backend-impl` (1 spawn) + `chairly-backend-qa` (1 spawn) + `chairly-frontend-qa` (1 spawn — also fixed frontend field renames)

### Rework Pass 3 (2026-03-23) — Service account + cancel dialog

**PR comments:**
1. GUIDs still showing for CancelledBy/ActivatedBy — previous user name resolution silently failing
2. Cancel subscription dialog has two buttons both labeled "Annuleren"

**Root cause (GUIDs):** `GetUserDisplayNameAsync` calls `GetAuthenticatedClientAsync(realmName)` which tries to get a service account token from the `chairly-admin` realm. But the `chairly-admin` service account client only exists in the **tenant** realm — the `chairly-admin` realm had no service account. Token request fails silently → falls back to GUID string.

**Fixes:**
- **Infra:** Added `chairly-admin` service account client to the `chairly-admin` Keycloak realm in `KeycloakDevSeeder.CreateAdminRealmAsync`, with `manage-users`/`manage-realm` roles assigned via `AssignServiceAccountRolesAsync`
- **Frontend:** Changed dismiss button from "Annuleren" to "Sluiten", confirm button from "Annuleren" to "Abonnement annuleren". Fixed e2e selector ambiguity for new button text.

**Agents:** `infra-impl` (1 spawn) + `frontend-impl` (1 spawn) + `chairly-backend-qa` (1 spawn) + `chairly-frontend-qa` (1 spawn)

---

## Overall Summary

| Workflow | Notes |
|----------|-------|
| Spec creation (interactive) | 8 user decisions, 10 review findings fixed across 2 review passes |
| Implementation | 2 infra + 6 backend + 9 frontend tasks, parallel execution |
| Code review | Backend 2 findings fixed, frontend 5 findings fixed |
| QA checks | All gates green |
| PR rework | 3 passes — Keycloak redirect, search+GUID display, service account+dialog |

### What was delivered

- **New backend:** 6 admin API endpoints (`GET /api/config/admin`, `GET /api/admin/subscriptions`, `GET /api/admin/subscriptions/{id}`, `POST .../provision`, `POST .../cancel`, `PUT .../plan`) protected by `RequirePlatformAdmin` policy
- **New frontend app:** `chairly-admin` Angular app with Keycloak auth (`chairly-admin` realm), subscription list page with search/filter/pagination/URL sync, subscription detail page with action dialogs
- **Infrastructure:** Keycloak `chairly-admin` realm with `platform_admin` role, service account client with `manage-users`/`manage-realm` roles, `TenantContextMiddleware` bypass for admin requests, Aspire AppHost config
- **Components:** Admin shell, subscription list page, subscription detail page, provision/cancel/update-plan dialog components, status badge pipe, billing cycle pipe
- **Tests:** 28 backend handler tests, 40 frontend unit tests, 56 Playwright e2e tests (Chromium + Firefox)
- **All UI in Dutch** — "Abonnementen", "Activeren", "Sluiten", "Abonnement annuleren", "Abonnement bijwerken", etc.

### Agents used

| Agent | Invocations | Purpose |
|-------|-------------|---------|
| spec-writer | 3 | Write spec interactively, apply review fixes (2 rounds) |
| spec-reviewer | 2 | Review spec (initial + second pass after fixes) |
| infra-impl | 3 | Implement I1 (Keycloak seeder) + I2 backend config + rework fixes (seeder order, service account) |
| infra-reviewer | 1 | Review infra implementation |
| backend-impl | 3 | Implement B1-B6 + code review fix pass + rework fix (search + user names) |
| frontend-impl | 3 | Implement I2 frontend + F1-F9 + code review fix pass + rework fix (cancel dialog) |
| backend-reviewer | 2 | Review backend (initial + re-review) |
| frontend-reviewer | 2 | Review frontend (initial + re-review) |
| chairly-backend-qa | 4 | Backend quality gate (implementation + 3 rework passes) |
| chairly-frontend-qa | 3 | Frontend quality gate (implementation + 2 rework passes) |
| **Total agent spawns** | **26** | |
