# Subscriptions

## Overview

Chairly transitions from a manual sign-up / demo-request onboarding model to a subscription-based SaaS model. The existing `DemoRequest` and `SignUpRequest` entities and flows are removed entirely. In their place, a new `Subscription` entity is introduced in the Onboarding bounded context. Subscription plans are tiered by staff count (Starter, Team, Salon) with monthly and annual billing cycles (10% annual discount). A free 30-day trial is modelled as a Subscription on the Starter plan with a trial period. The public website is updated: the "Demo aanvragen" page is deleted, the "Aanmelden" page is replaced by a unified subscribe/trial flow (plan selection cards followed by a user-info form), a dedicated pricing page is added at `/prijzen`, and a pricing summary section is added to the landing page. Payment integration (Stripe) is out of scope but the data model and form flow are designed to accommodate it later.

## Domain Context

- Bounded context: **Onboarding** (existing, reworked)
- Key entities involved: **Subscription** (new, replaces SignUpRequest), **SubscriptionPlan** (new enum)
- Entities removed: **DemoRequest**, **SignUpRequest**
- Ubiquitous language:
  - **Subscription** -- a tenant's contract to use the Chairly platform, with a plan, billing cycle, and optional trial period
  - **SubscriptionPlan** -- a tier defining staff limits and pricing: Starter, Team, or Salon
  - **BillingCycle** -- monthly or annual billing frequency
  - **Trial** -- a free 30-day Subscription on the Starter plan; modelled as a Subscription with `TrialEndsAtUtc` set. Trial status is derived from `TrialEndsAtUtc != null` (no separate boolean flag, per ADR-009).
  - **Subscriber** -- the person (future tenant owner) who creates a Subscription

### Subscription Plans

| Plan | Slug | Max Staff | Monthly Price | Annual Price (per month) |
|------|------|-----------|---------------|--------------------------|
| Starter | `starter` | 1 | EUR 14,99 | EUR 13,49 |
| Team | `team` | 5 | EUR 59,99 | EUR 53,99 |
| Salon | `salon` | 15 | EUR 149,00 | EUR 134,10 |

Annual pricing = monthly price * 0.90, billed as a lump sum yearly.

### Business Rules

- A trial is a Subscription with `Plan = Starter` and `TrialEndsAtUtc = CreatedAtUtc + 30 days`. Trial status is derived: `TrialEndsAtUtc != null` means trial.
- Trials have no billing cycle or price (free). `BillingCycle` is null for trials.
- A paid Subscription has a `BillingCycle` of `Monthly` or `Annual` and `TrialEndsAtUtc = null`.
- `SubscriptionPlan` is an enum (Starter, Team, Salon) -- plan metadata (prices, limits) is defined in code, not in the database. This keeps it simple and avoids a lookup table for three static values.
- `BillingCycle` is an enum (Monthly, Annual).
- The backend stores subscription requests and sends an admin notification email (same pattern as the old sign-up flow). Actual provisioning remains manual.
- The form collects: salon name, owner first name, owner last name, email, phone (same fields as the old sign-up form, needed for future Stripe integration).
- Status is derived from timestamps (ADR-009):
  - **Pending**: `CreatedAtUtc` set, nothing else
  - **Provisioned**: `ProvisionedAtUtc` set
  - **Cancelled**: `CancelledAtUtc` set

---

## Backend Tasks

### B1 — Remove DemoRequest entity and flow

Delete all code related to the DemoRequest feature.

**Files to delete:**
- `Chairly.Domain/Entities/DemoRequest.cs`
- `Chairly.Domain/Events/DemoRequestSubmittedEvent.cs`
- `Chairly.Infrastructure/Persistence/Configurations/Website/DemoRequestConfiguration.cs`
- `Chairly.Api/Features/Onboarding/SubmitDemoRequest/SubmitDemoRequestCommand.cs`
- `Chairly.Api/Features/Onboarding/SubmitDemoRequest/SubmitDemoRequestHandler.cs`
- `Chairly.Api/Features/Onboarding/SubmitDemoRequest/SubmitDemoRequestEndpoint.cs`
- `Chairly.Api/Features/Onboarding/SubmitDemoRequestResponse.cs`

**Files to update:**
- `Chairly.Infrastructure/Persistence/WebsiteDbContext.cs` -- remove `DbSet<DemoRequest>` property
- `Chairly.Infrastructure/Messaging/IOnboardingEventPublisher.cs` -- remove `PublishDemoRequestSubmittedAsync` method
- `Chairly.Api/Features/Onboarding/OnboardingEventPublisher.cs` -- remove `PublishDemoRequestSubmittedAsync` implementation
- `Chairly.Api/Features/Onboarding/OnboardingEndpoints.cs` -- remove `app.MapSubmitDemoRequest()` call
- `Chairly.Tests/Features/Onboarding/OnboardingHandlerTests.cs` -- remove demo-request tests

**Migration:** Add a new migration `RemoveDemoRequests` that drops the `DemoRequests` table. Use raw SQL: `DROP TABLE IF EXISTS "DemoRequests";`. Do NOT remove the old migration file -- only add a new one.

**Tests:**
- Verify the `/api/onboarding/demo-requests` endpoint no longer exists (404)

---

### B2 — Remove SignUpRequest entity and flow

Delete all code related to the SignUpRequest feature.

**Files to delete:**
- `Chairly.Domain/Entities/SignUpRequest.cs`
- `Chairly.Domain/Events/SignUpRequestSubmittedEvent.cs`
- `Chairly.Infrastructure/Persistence/Configurations/Website/SignUpRequestConfiguration.cs`
- `Chairly.Api/Features/Onboarding/SubmitSignUpRequest/SubmitSignUpRequestCommand.cs`
- `Chairly.Api/Features/Onboarding/SubmitSignUpRequest/SubmitSignUpRequestHandler.cs`
- `Chairly.Api/Features/Onboarding/SubmitSignUpRequest/SubmitSignUpRequestEndpoint.cs`
- `Chairly.Api/Features/Onboarding/SubmitSignUpRequestResponse.cs`

**Files to update:**
- `Chairly.Infrastructure/Persistence/WebsiteDbContext.cs` -- remove `DbSet<SignUpRequest>` property
- `Chairly.Infrastructure/Messaging/IOnboardingEventPublisher.cs` -- remove `PublishSignUpRequestSubmittedAsync` method
- `Chairly.Api/Features/Onboarding/OnboardingEventPublisher.cs` -- remove `PublishSignUpRequestSubmittedAsync` implementation
- `Chairly.Api/Features/Onboarding/OnboardingEndpoints.cs` -- remove `app.MapSubmitSignUpRequest()` call
- `Chairly.Tests/Features/Onboarding/OnboardingHandlerTests.cs` -- remove sign-up-request tests

**Migration:** Add to the same migration as B1 (or a separate one `RemoveSignUpRequests`): `DROP TABLE IF EXISTS "SignUpRequests";`.

**Tests:**
- Verify the `/api/onboarding/sign-up-requests` endpoint no longer exists (404)

---

### B3 — Subscription entity, enums, EF configuration, and migration

Create the new `Subscription` entity and supporting enums.

**Domain -- `Chairly.Domain/Enums/SubscriptionPlan.cs`:**
```csharp
public enum SubscriptionPlan
{
    Starter = 0,
    Team = 1,
    Salon = 2,
}
```

**Domain -- `Chairly.Domain/Enums/BillingCycle.cs`:**
```csharp
public enum BillingCycle
{
    Monthly = 0,
    Annual = 1,
}
```

**Domain -- `Chairly.Domain/Entities/Subscription.cs`:**

Note: There is no `IsTrial` boolean. Trial status is derived from `TrialEndsAtUtc != null` (ADR-009 -- no redundant status columns).

```csharp
public class Subscription
{
    public Guid Id { get; set; }
    public string SalonName { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public BillingCycle? BillingCycle { get; set; }         // null for trials
    public DateTimeOffset? TrialEndsAtUtc { get; set; }     // non-null = trial; null = paid
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }                    // nullable (anonymous submission)
    public DateTimeOffset? ProvisionedAtUtc { get; set; }
    public Guid? ProvisionedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    // Derived property -- not persisted
    public bool IsTrial => TrialEndsAtUtc is not null;
}
```

**EF Configuration -- `Chairly.Infrastructure/Persistence/Configurations/Website/SubscriptionConfiguration.cs`:**
- Table: `Subscriptions`
- `SalonName` required, max 200
- `OwnerFirstName` required, max 100
- `OwnerLastName` required, max 100
- `Email` required, max 256
- `PhoneNumber` optional, max 50
- `Plan` stored as string via `.HasConversion<string>()`, max 20
- `BillingCycle` stored as string via `.HasConversion<string>()`, max 20, nullable
- `CancellationReason` optional, max 1000
- `IsTrial` must be ignored: `builder.Ignore(s => s.IsTrial);`
- Indexes: `Email`, `CreatedAtUtc`, `Plan`

**WebsiteDbContext:** Add `DbSet<Subscription> Subscriptions => Set<Subscription>();`

**Migration:** `AddSubscriptions` -- idempotent:
- `CREATE TABLE IF NOT EXISTS "Subscriptions" (...)` via raw SQL
- `CREATE INDEX IF NOT EXISTS` for Email, CreatedAtUtc, Plan indexes
- No `IsTrial` column in the database

**Tests:**
- Subscription entity can be persisted and retrieved
- Plan and BillingCycle are stored as strings
- Trial subscriptions (TrialEndsAtUtc set) have null BillingCycle; `IsTrial` returns true
- Paid subscriptions (TrialEndsAtUtc null) have non-null BillingCycle; `IsTrial` returns false

---

### B4 — CreateSubscription command, handler, and endpoint

Create the slice for creating a new subscription (or starting a trial).

**Slice:** `Chairly.Api/Features/Onboarding/CreateSubscription/`

**Command -- `CreateSubscriptionCommand.cs`:**

The command accepts `Plan` as a **string slug** (not the enum directly) to match the frontend contract. The handler converts the slug to the `SubscriptionPlan` enum.

```csharp
internal sealed class CreateSubscriptionCommand : IRequest<OneOf<SubscriptionResponse, ValidationFailed>>
{
    [Required, MaxLength(200)]
    public string SalonName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Plan { get; set; } = string.Empty;           // "starter", "team", "salon"

    public string? BillingCycle { get; set; }                   // "Monthly", "Annual", or null (for trials)

    public bool IsTrial { get; set; }
}
```

**Validation rules (in handler, beyond Data Annotations):**
- `Plan` must parse to a valid `SubscriptionPlan` enum value (case-insensitive): "starter" -> Starter, "team" -> Team, "salon" -> Salon. Return `ValidationFailed` if invalid.
- `BillingCycle`, if not null, must parse to a valid `BillingCycle` enum value (case-insensitive): "Monthly" -> Monthly, "Annual" -> Annual. Return `ValidationFailed` if invalid.
- If `IsTrial` is true: parsed `Plan` must be `Starter`, `BillingCycle` must be null
- If `IsTrial` is false: `BillingCycle` is required (cannot be null)

**Handler -- `CreateSubscriptionHandler.cs`:**
1. Parse `Plan` string to `SubscriptionPlan` enum (case-insensitive); return `ValidationFailed` if invalid
2. Parse `BillingCycle` string to `BillingCycle` enum if not null; return `ValidationFailed` if invalid
3. Validate business rules above; return `ValidationFailed` if violated
4. Create `Subscription` entity:
   - Set `Plan` (enum), `BillingCycle` (enum, nullable) from parsed values
   - If `IsTrial`: set `TrialEndsAtUtc = DateTimeOffset.UtcNow.AddDays(30)`, leave `BillingCycle` null
   - If not trial: leave `TrialEndsAtUtc` null
   - Set `CreatedAtUtc = DateTimeOffset.UtcNow`, `CreatedBy = null` (anonymous)
5. Persist to `WebsiteDbContext`
6. Publish `SubscriptionCreatedEvent` via `IOnboardingEventPublisher` (admin notification email)
7. Return `SubscriptionResponse`

**Endpoint -- `CreateSubscriptionEndpoint.cs`:**
- `POST /api/onboarding/subscriptions`
- Request body: `CreateSubscriptionCommand`
- 201 Created with `SubscriptionResponse` body
- 422 Unprocessable Entity for validation failures

**Response DTO -- `Chairly.Api/Features/Onboarding/SubscriptionResponse.cs`:**

The response serializes `Plan` as a lowercase slug and `BillingCycle` as the enum name (PascalCase) to match the frontend contract.

```csharp
internal sealed record SubscriptionResponse(
    Guid Id,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string Plan,                    // "starter", "team", "salon"
    string? BillingCycle,           // "Monthly", "Annual", or null
    bool IsTrial,                   // derived from TrialEndsAtUtc
    DateTimeOffset? TrialEndsAtUtc,
    DateTimeOffset CreatedAtUtc);
```

When constructing the response from the entity, map: `Plan = subscription.Plan.ToString().ToLowerInvariant()`, `BillingCycle = subscription.BillingCycle?.ToString()`, `IsTrial = subscription.IsTrial`.

**Domain event -- `Chairly.Domain/Events/SubscriptionCreatedEvent.cs`:**
```csharp
public record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    string SalonName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string? PhoneNumber,
    string Plan,
    string? BillingCycle,
    bool IsTrial);
```

**Update `IOnboardingEventPublisher`:**
- Replace old methods with: `Task PublishSubscriptionCreatedAsync(SubscriptionCreatedEvent domainEvent, CancellationToken cancellationToken = default);`

**Update `OnboardingEventPublisher`:**
- Implement `PublishSubscriptionCreatedAsync`: send admin notification email with subscription details (plan, trial/paid, billing cycle, salon name, owner, email, phone)
- Email subject: `"Nieuw abonnement: {SalonName}"` for paid, `"Nieuwe proefperiode: {SalonName}"` for trial

**Update `OnboardingEndpoints.cs`:**
- Remove old method calls (already done in B1/B2)
- Add `app.MapCreateSubscription();`

**Tests:**
- Creating a trial subscription sets TrialEndsAtUtc approximately 30 days from now, Plan=Starter, BillingCycle=null; response IsTrial=true
- Creating a paid subscription sets TrialEndsAtUtc=null, BillingCycle=Monthly or Annual; response IsTrial=false
- Trial with plan="team" returns 422
- Trial with billingCycle="Monthly" returns 422
- Paid subscription without billingCycle returns 422
- Invalid plan string (e.g. "enterprise") returns 422
- Invalid billingCycle string (e.g. "Weekly") returns 422
- Missing required fields return 422
- Admin notification email is sent
- Response plan field is lowercase string ("starter", not "Starter")

---

### B5 — GetSubscriptionPlans endpoint (static plan metadata)

Expose plan metadata as an API endpoint so the frontend can fetch pricing info dynamically (avoids hardcoding prices in the frontend).

**Slice:** `Chairly.Api/Features/Onboarding/GetSubscriptionPlans/`

**Query -- `GetSubscriptionPlansQuery.cs`:**
```csharp
internal sealed class GetSubscriptionPlansQuery : IRequest<IReadOnlyList<SubscriptionPlanResponse>> { }
```

**Handler -- `GetSubscriptionPlansHandler.cs`:**
Return a static list of plans:
```csharp
[
    new("starter", "Starter", 1, 14.99m, 13.49m),
    new("team", "Team", 5, 59.99m, 53.99m),
    new("salon", "Salon", 15, 149.00m, 134.10m),
]
```

**Response DTO -- `SubscriptionPlanResponse.cs` (in the Onboarding feature root):**
```csharp
internal sealed record SubscriptionPlanResponse(
    string Slug,
    string Name,
    int MaxStaff,
    decimal MonthlyPrice,
    decimal AnnualPricePerMonth);
```

**Endpoint -- `GetSubscriptionPlansEndpoint.cs`:**
- `GET /api/onboarding/plans`
- 200 OK with `IReadOnlyList<SubscriptionPlanResponse>`
- No authentication required (public endpoint)

**Update `OnboardingEndpoints.cs`:** Add `app.MapGetSubscriptionPlans();`

**Tests:**
- Returns 3 plans
- Plans are in order: starter, team, salon
- Prices match expected values
- MaxStaff values are correct

---

## Frontend Tasks

### F1 — Remove demo-request and sign-up pages, update models

Delete all frontend code related to the demo-request and sign-up flows.

**Files to delete:**
- `libs/website/src/lib/onboarding/feature/demo-request-page/` (entire directory)
- `libs/website/src/lib/onboarding/feature/sign-up-page/` (entire directory)
- `libs/website/src/lib/onboarding/models/demo-request.model.ts`
- `libs/website/src/lib/onboarding/models/sign-up-request.model.ts`
- `apps/chairly-website-e2e/src/demo-request.spec.ts`
- `apps/chairly-website-e2e/src/sign-up.spec.ts`

**Files to update:**
- `libs/website/src/lib/onboarding/feature/index.ts` -- remove exports for `DemoRequestPageComponent` and `SignUpPageComponent`
- `libs/website/src/lib/onboarding/models/index.ts` -- remove exports for demo-request and sign-up models
- `libs/website/src/lib/onboarding/onboarding.routes.ts` -- remove `demo-aanvragen` and `aanmelden` routes
- `libs/website/src/lib/onboarding/data-access/onboarding-api.service.ts` -- remove `submitDemoRequest()` and `submitSignUpRequest()` methods

---

### F2 — Subscription models and API service

Create new TypeScript models and update the API service for the subscription flow.

**New file -- `libs/website/src/lib/onboarding/models/subscription.model.ts`:**
```typescript
export interface SubscriptionPlanInfo {
  slug: string;
  name: string;
  maxStaff: number;
  monthlyPrice: number;
  annualPricePerMonth: number;
}

export interface CreateSubscriptionPayload {
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
  plan: string;                 // 'starter' | 'team' | 'salon'
  billingCycle: string | null;  // 'Monthly' | 'Annual' | null (for trials)
  isTrial: boolean;
}

export interface SubscriptionResponse {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;             // derived on the backend from trialEndsAtUtc
  trialEndsAtUtc: string | null;
  createdAtUtc: string;
}
```

**Update `libs/website/src/lib/onboarding/models/index.ts`:**
Export all new types from `subscription.model.ts`.

**Update `libs/website/src/lib/onboarding/data-access/onboarding-api.service.ts`:**
```typescript
getSubscriptionPlans(): Observable<SubscriptionPlanInfo[]> {
  return this.http.get<SubscriptionPlanInfo[]>('/api/onboarding/plans');
}

createSubscription(payload: CreateSubscriptionPayload): Observable<SubscriptionResponse> {
  return this.http.post<SubscriptionResponse>('/api/onboarding/subscriptions', payload);
}
```

---

### F3 — Pricing page at /prijzen

Create a dedicated pricing page with plan cards, feature comparison, and billing cycle toggle.

**New files:**
```
libs/website/src/lib/onboarding/feature/pricing-page/
  pricing-page.component.ts
  pricing-page.component.html
  pricing-page.component.scss
  pricing-page.component.spec.ts
```

**New UI component -- pricing card:**
```
libs/website/src/lib/onboarding/ui/pricing-card/
  pricing-card.component.ts
  pricing-card.component.html
  pricing-card.component.scss
  pricing-card.component.spec.ts
```

**PricingCardComponent (presentational, `ui/pricing-card/`):**
- Uses signal-based API per CLAUDE.md conventions (no `@Input()`/`@Output()` decorators):
  - `plan = input.required<SubscriptionPlanInfo>()`
  - `billingCycle = input.required<'monthly' | 'annual'>()`
  - `highlighted = input<boolean>(false)`
  - `isTrial = input<boolean>(false)`
  - `selectPlan = new OutputEmitterRef<void>()` (output)
- Displays: plan name, price based on billing cycle, max staff description, CTA button
- The trial card variant shows "Gratis proberen" with "30 dagen gratis" subtext instead of a price
- The highlighted card (Team, recommended) gets a visual emphasis (border/badge "Populair")
- CTA button text:
  - Trial card: "Gratis starten"
  - Paid cards: "Abonnement kiezen"
- All text in Dutch

**PricingPageComponent (smart, `feature/pricing-page/`):**
- `ChangeDetectionStrategy.OnPush`, standalone
- Fetches plans from `OnboardingApiService.getSubscriptionPlans()` on init
- Signals: `plans`, `isLoading`, `billingCycle` (toggle between 'monthly' and 'annual')
- Billing cycle toggle: "Maandelijks" / "Jaarlijks (10% korting)" radio or toggle switch
- Displays plan cards in a responsive grid (1 col mobile, 4 cols desktop)
- Adds a "Gratis proberen" trial card as the first card
- When a plan card CTA is clicked, navigates to `/abonneren?plan={slug}&trial={true|false}&cyclus={monthly|annual}`
- Below the plan cards: a feature comparison table showing what is included per plan
- Feature comparison items (all plans include all features -- the difference is staff count):
  - "Boekingen beheren" -- check all
  - "Klantenbeheer" -- check all
  - "Facturatie" -- check all
  - "Automatische meldingen" -- check all
  - "Aantal medewerkers" -- 1 / 5 / 15
- Page heading: "Kies het plan dat bij uw salon past"
- Subheading: "Alle plannen bevatten dezelfde functionaliteiten. U betaalt alleen voor het aantal medewerkers."

**SEO:**
- `<title>`: "Prijzen - Chairly | Salon software abonnementen"
- `<meta name="description">`: "Bekijk de abonnementsprijzen van Chairly. Kies uit Starter, Team of Salon. Alle plannen bevatten boekingen, klantenbeheer, facturatie en meldingen. 30 dagen gratis proberen."
- `<meta property="og:title">`: "Prijzen - Chairly"
- `<meta property="og:description">`: "Eenvoudige, transparante prijzen voor salon software. Vanaf EUR 14,99 per maand. Probeer 30 dagen gratis."
- Set meta tags via Angular `Meta` service in the component's constructor or `OnInit`.
- Ensure proper heading hierarchy: `<h1>` for main heading, `<h2>` for section headings (FAQ, comparison table), `<h3>` for FAQ question items.

**Template structure:**
```
<chairly-web-header />
<main>
  <section> <!-- Hero/heading -->
    <h1>Kies het plan dat bij uw salon past</h1>
    <p>Alle plannen bevatten dezelfde functionaliteiten...</p>
    <billing-cycle-toggle />
  </section>
  <section> <!-- Plan cards (trial + 3 paid plans) -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-4">
      <!-- Trial card -->
      <chairly-web-pricing-card [isTrial]="true" ... />
      <!-- Starter, Team (highlighted), Salon -->
      <chairly-web-pricing-card ... />
    </div>
  </section>
  <section> <!-- Feature comparison table -->
    <h2>Vergelijk plannen</h2>
    <table>...</table>
  </section>
  <section> <!-- FAQ -->
    <h2>Veelgestelde vragen</h2>
  </section>
</main>
<chairly-web-footer />
```

**FAQ items (Dutch):**
- "Hoe werkt de gratis proefperiode?" -- "U kunt Chairly 30 dagen gratis uitproberen met het Starter-plan. Geen betaalgegevens nodig."
- "Kan ik van plan wisselen?" -- "Ja, u kunt op elk moment upgraden of downgraden."
- "Hoe werkt de facturatie?" -- "Bij een maandabonnement betaalt u elke maand. Bij een jaarabonnement betaalt u vooruit voor 12 maanden met 10% korting."
- "Kan ik mijn abonnement opzeggen?" -- "Ja, u kunt op elk moment opzeggen. Uw account blijft actief tot het einde van de betaalperiode."

**Route:** Add `{ path: 'prijzen', loadComponent: ... }` to `onboarding.routes.ts`

**Update feature/index.ts:** Export `PricingPageComponent`

**Update ui/index.ts:** Export `PricingCardComponent`

---

### F4 — Subscribe page at /abonneren (plan selection + user info form)

Create the unified subscription/trial sign-up page. This replaces the old `/aanmelden` page.

**New files:**
```
libs/website/src/lib/onboarding/feature/subscribe-page/
  subscribe-page.component.ts
  subscribe-page.component.html
  subscribe-page.component.scss
  subscribe-page.component.spec.ts
```

**New UI component -- subscribe form:**
```
libs/website/src/lib/onboarding/ui/subscribe-form/
  subscribe-form.component.ts
  subscribe-form.component.html
  subscribe-form.component.scss
  subscribe-form.component.spec.ts
```

**SubscribeFormComponent (presentational, `ui/subscribe-form/`):**
- Uses signal-based API per CLAUDE.md conventions (no `@Input()`/`@Output()` decorators):
  - `selectedPlan = input.required<SubscriptionPlanInfo>()`
  - `billingCycle = input.required<'monthly' | 'annual'>()`
  - `isTrial = input.required<boolean>()`
  - `isSubmitting = input<boolean>(false)`
  - `submitError = input<string | null>(null)`
  - `formSubmit = new OutputEmitterRef<CreateSubscriptionPayload>()` (output)
- Reactive form fields:
  - `salonName` -- required, max 200. Label: "Salonnaam"
  - `ownerFirstName` -- required, max 100. Label: "Voornaam"
  - `ownerLastName` -- required, max 100. Label: "Achternaam"
  - `email` -- required, email, max 256. Label: "E-mailadres"
  - `phoneNumber` -- optional, max 50. Label: "Telefoonnummer"
- Shows a summary panel at the top: selected plan name, price, billing cycle (or "30 dagen gratis" for trials)
- For paid plans, shows a note: "Betaling wordt later ingesteld" (placeholder for future Stripe integration)
- Submit button text: "Proefperiode starten" for trials, "Abonnement aanmaken" for paid
- Validation messages in Dutch (same pattern as current sign-up form)
- On submit: emits `CreateSubscriptionPayload` with `plan`, `billingCycle`, `isTrial` populated from inputs

**SubscribePageComponent (smart, `feature/subscribe-page/`):**
- `ChangeDetectionStrategy.OnPush`, standalone
- Reads query params: `plan`, `trial`, `cyclus`
- Fetches plans from `OnboardingApiService.getSubscriptionPlans()` to resolve the selected plan info
- If no valid plan query param, redirects to `/prijzen`
- Signals: `selectedPlan`, `billingCycle`, `isTrial`, `isSubmitting`, `submitError`, `plans`, `isLoading`
- On form submit: calls `OnboardingApiService.createSubscription()`, navigates to `/bevestiging?type=abonnement` on success
- Page heading: "Uw gegevens" for paid, "Start uw gratis proefperiode" for trial
- Subheading: "Vul uw gegevens in om uw {plan.name}-abonnement te activeren." for paid, "Vul uw gegevens in om 30 dagen gratis te starten met Chairly." for trial

**Route:** Add `{ path: 'abonneren', loadComponent: ... }` to `onboarding.routes.ts`

**Update feature/index.ts:** Export `SubscribePageComponent`

**Update ui/index.ts:** Export `SubscribeFormComponent`

---

### F5 — Update landing page with pricing section and new CTAs

Modify the existing landing page to replace demo/sign-up references and add a pricing summary.

**Update hero section inputs on landing page (`landing-page.component.html`):**
- `primaryCtaLabel`: "Gratis proberen" (was "Demo aanvragen")
- `primaryCtaLink`: "/abonneren?plan=starter&trial=true" (was "/demo-aanvragen")
- `secondaryCtaLabel`: "Bekijk prijzen" (was "Nu aanmelden")
- `secondaryCtaLink`: "/prijzen" (was "/aanmelden")

**Add pricing summary section** between "Waarom Chairly?" and "Social proof" sections:
- Heading: "Eenvoudige, transparante prijzen"
- Subheading: "Geen verborgen kosten. Kies het plan dat past bij uw salon."
- Show 3 plan summary cards (name, starting price "Vanaf EUR X,XX / maand", staff count, CTA)
- Plus a trial callout: "Of probeer Chairly 30 dagen gratis" with a "Gratis starten" link
- "Bekijk alle prijzen" link to `/prijzen`
- This section does NOT fetch from the API -- uses hardcoded values (same as the rest of the landing page is static marketing content)

**Update CTA section** at the bottom of the landing page:
- Replace "Demo aanvragen" button with "Bekijk prijzen" linking to `/prijzen`
- Replace "Gratis proberen" button with "Gratis starten" linking to `/abonneren?plan=starter&trial=true`
- Update heading: keep "Klaar om te starten?"
- Update subheading: "Start vandaag nog met uw gratis proefperiode van 30 dagen."

**Update `LandingPageComponent` TS:** Add `RouterLink` import if not already present (it is).

---

### F6 — Update header, footer, and confirmation page

Update shared website components to reflect the new navigation structure.

**Update header (`ui/header/`):**

Desktop navigation:
- "Home" -- `/` (keep)
- "Prijzen" -- `/prijzen` (replaces "Demo aanvragen")
- "Gratis proberen" -- `/abonneren?plan=starter&trial=true` (styled as primary button, replaces "Aanmelden")

Mobile navigation:
- "Home" -- `/` (keep)
- "Prijzen" -- `/prijzen` (replaces "Demo aanvragen")
- "Gratis proberen" -- `/abonneren?plan=starter&trial=true` (styled as primary button, replaces "Aanmelden")

**Update confirmation page (`feature/confirmation-page/`):**
- Remove `isDemo` and `isSignUp` computed properties
- Add `isSubscription` computed: `type() === 'abonnement'`
- Heading: "Bedankt voor uw aanmelding!"
- Message: "Wij verwerken uw aanvraag zo snel mogelijk. U ontvangt een e-mail zodra uw omgeving klaar is."
- Keep the generic fallback for unknown types
- The confirmation page can be simplified to a single message since demo requests no longer exist

**Update footer:** No changes needed (footer has no references to demo/sign-up).

---

### F7 — E2E tests for subscription flows

Write Playwright e2e tests for all new pages and updated flows.

**Update `apps/chairly-website-e2e/src/landing-page.spec.ts`:**
- Update test: hero section should show "Gratis proberen" primary CTA
- Update test: clicking "Gratis proberen" navigates to `/abonneren?plan=starter&trial=true`
- Update test: clicking "Bekijk prijzen" navigates to `/prijzen`
- Add test: pricing summary section is visible on landing page
- Add test: header shows "Prijzen" and "Gratis proberen" links

**New file `apps/chairly-website-e2e/src/pricing.spec.ts`:**
- Navigate to `/prijzen`
- Verify heading "Kies het plan dat bij uw salon past" is visible
- Verify 4 plan cards are shown (trial + 3 paid)
- Verify billing cycle toggle switches prices
- Click trial "Gratis starten" button, verify navigation to `/abonneren?plan=starter&trial=true`
- Click paid plan CTA, verify navigation to `/abonneren?plan={slug}&trial=false&cyclus=monthly`
- Verify feature comparison table is visible
- Verify FAQ section is visible

**New file `apps/chairly-website-e2e/src/subscribe.spec.ts`:**
- Mock `GET /api/onboarding/plans` to return plan data
- Mock `POST /api/onboarding/subscriptions` to return success
- Navigate to `/abonneren?plan=starter&trial=true`
- Verify trial heading "Start uw gratis proefperiode" is visible
- Fill in form, submit, verify navigation to `/bevestiging?type=abonnement`
- Navigate to `/abonneren?plan=team&trial=false&cyclus=monthly`
- Verify paid heading "Uw gegevens" is visible
- Verify plan summary shows "Team" plan with monthly price
- Test validation errors (empty required fields)
- Navigate to `/abonneren` without query params, verify redirect to `/prijzen`

---

## Acceptance Criteria

- [ ] `DemoRequest` entity, EF config, migration table, endpoints, handlers, tests, events, and all references are removed
- [ ] `SignUpRequest` entity, EF config, migration table, endpoints, handlers, tests, events, and all references are removed
- [ ] `Subscription` entity exists with `Plan`, `BillingCycle`, `TrialEndsAtUtc`, and timestamp pairs (no `IsTrial` column -- derived from `TrialEndsAtUtc`)
- [ ] `SubscriptionPlan` and `BillingCycle` enums exist in `Chairly.Domain/Enums/`
- [ ] `POST /api/onboarding/subscriptions` accepts plan as string slug, creates a subscription (trial or paid), and returns 201
- [ ] Trial validation: must be Starter plan, no billing cycle
- [ ] Paid validation: billing cycle required
- [ ] `GET /api/onboarding/plans` returns 3 plans with correct pricing
- [ ] Admin notification email sent on subscription creation
- [ ] Pricing page at `/prijzen` displays plan cards with billing cycle toggle
- [ ] Pricing page has proper SEO meta tags (title, description, og:title, og:description) in Dutch
- [ ] Subscribe page at `/abonneren` shows plan summary + user info form
- [ ] Trial flow: `/abonneren?plan=starter&trial=true` starts a 30-day trial
- [ ] Landing page hero CTAs updated: "Gratis proberen" and "Bekijk prijzen"
- [ ] Landing page has pricing summary section
- [ ] Header nav updated: "Prijzen" link and "Gratis proberen" button
- [ ] Confirmation page works for subscription type
- [ ] All user-facing text is in Dutch
- [ ] All frontend components use signal-based API (`input()`, `OutputEmitterRef`) -- no `@Input()`/`@Output()` decorators
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass for pricing page, subscribe page, and updated landing page

## Out of Scope

- Stripe payment integration (future spec -- the form and data model are designed to accommodate it)
- Subscription management portal (upgrading, downgrading, cancelling via UI)
- Automated tenant provisioning based on subscription
- Trial expiry enforcement / automated reminders
- Usage-based billing or overage charges
- Custom/enterprise plan tier
- Admin dashboard for managing subscriptions
- Email verification during sign-up
- Terms of service / privacy policy pages (footer links remain as placeholder `#` hrefs)
