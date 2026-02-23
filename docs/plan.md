# AI-First Development Plan: Salon Management SaaS

## Overzicht

Dit plan beschrijft een gestructureerde aanpak om met Claude Code een volledig Salon Management SaaS product op te bouwen. De kern is: **jij bent de architect en product owner, Claude Code is je senior developer.**

---

## Fase 0: Projectfundament (voordat je code schrijft)

### 0.1 — Product Discovery & Domain Model

Voordat je Claude Code ook maar één regel code laat schrijven, definieer je het domein. Dit is het belangrijkste document in je hele project.

Maak een `docs/domain-model.md` met:

- **Bounded Contexts** (DDD-stijl): bijv. `Appointments`, `Clients`, `Staff`, `Services`, `Billing`, `Notifications`
- **Core Entities** per context met hun relaties
- **Ubiquitous Language**: een glossary zodat Claude Code consistent dezelfde termen gebruikt (bijv. "Appointment" vs "Booking" — kies er één)
- **User Roles**: Owner, Staff Member, Client (als je een client portal bouwt)

### 0.2 — Architecture Decision Records (ADRs)

Maak een `docs/adr/` folder. Leg je technische keuzes vast zodat Claude Code ze kan raadplegen:

| ADR | Beslissing |
|-----|-----------|
| ADR-001 | Monorepo met Nx |
| ADR-002 | .NET Aspire als orchestrator voor local dev |
| ADR-003 | PostgreSQL met EF Core (code-first migrations) |
| ADR-004 | RabbitMQ voor async events (bijv. appointment reminders) |
| ADR-005 | CQRS-light: MediatR voor commands/queries, geen event sourcing |
| ADR-006 | Angular met standalone components, signals, NgRx SignalStore |
| ADR-007 | Multi-tenancy strategie (schema-per-tenant vs row-level) |
| ADR-008 | Auth strategie (bijv. Keycloak, Auth0, of ASP.NET Identity) |

---

## Fase 1: Repository & Tooling Setup

### 1.1 — Monorepo Structuur

```
salon-saas/
├── .claude/
│   ├── instructions.md          # Globale Claude Code instructies
│   └── commands/                 # Custom slash commands
│       ├── implement-feature.md
│       ├── write-tests.md
│       └── create-migration.md
├── docs/
│   ├── domain-model.md
│   ├── adr/
│   └── specs/                    # Feature specificaties
│       ├── _template.md
│       ├── appointment-booking.md
│       └── client-management.md
├── src/
│   ├── backend/
│   │   ├── SalonSaas.sln
│   │   ├── SalonSaas.AppHost/          # .NET Aspire host
│   │   ├── SalonSaas.ServiceDefaults/  # Shared Aspire config
│   │   ├── SalonSaas.Api/              # WebAPI project
│   │   ├── SalonSaas.Application/      # CQRS handlers, services
│   │   ├── SalonSaas.Domain/           # Entities, value objects
│   │   ├── SalonSaas.Infrastructure/   # EF Core, RabbitMQ, etc.
│   │   └── SalonSaas.Tests/            # Unit + integration tests
│   └── frontend/
│       └── salon-app/                  # Nx Angular workspace
├── docker/
│   ├── docker-compose.yml
│   └── Dockerfile.api
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── deploy.yml
├── .editorconfig
├── .prettierrc
├── .eslintrc.json
├── nx.json
└── CLAUDE.md                     # Root-level Claude instructies
```

### 1.2 — CLAUDE.md (Root Instructies)

Dit is het belangrijkste bestand voor je AI-first workflow. Claude Code leest dit automatisch.

```markdown
# CLAUDE.md

## Project
Salon Management SaaS - Multi-tenant platform voor kapsalons en schoonheidssalons.

## Architectuur
- Lees `docs/domain-model.md` voor het domeinmodel
- Lees `docs/adr/` voor architectuurbeslissingen
- Feature specs staan in `docs/specs/`

## Tech Stack
- Backend: .NET 9, ASP.NET Core Web API, EF Core, MediatR
- Frontend: Angular 19, Nx, NgRx SignalStore, Tailwind CSS
- Infra: PostgreSQL, RabbitMQ, .NET Aspire, Docker

## Code Conventies — Backend
- Volg Clean Architecture: Domain → Application → Infrastructure → Api
- Gebruik MediatR voor alle commands en queries
- Commands: `Create{Entity}Command`, `Update{Entity}Command`, `Delete{Entity}Command`
- Queries: `Get{Entity}Query`, `Get{Entities}ListQuery`
- Handlers altijd in `Application/Features/{Context}/{Commands|Queries}/`
- Gebruik FluentValidation voor input validatie
- Entity configuraties in aparte `IEntityTypeConfiguration<T>` classes
- Alle endpoints via Minimal APIs, gegroepeerd per feature in extension methods
- Gebruik Result pattern (geen exceptions voor business logic)
- Test coverage: Unit tests voor handlers, integration tests voor API endpoints

## Code Conventies — Frontend
- Standalone components, geen NgModules
- Gebruik Angular signals en NgRx SignalStore voor state
- Smart/Dumb component pattern: containers laden data, presentational components tonen het
- Services voor API calls, één service per backend context
- Gebruik Tailwind CSS, geen custom SCSS tenzij absoluut nodig
- Reactive forms met typed FormGroups
- Lazy-loaded routes per feature module

## Workflow
1. Lees altijd eerst de relevante spec in `docs/specs/` voordat je begint
2. Maak eerst de domain entities en value objects
3. Dan de EF Core configuratie en migratie
4. Dan de MediatR handlers met FluentValidation
5. Dan de API endpoints
6. Dan de Angular feature (service → store → components → routes)
7. Schrijf tests bij elke stap
8. Commit met conventional commits: feat(appointments): add booking endpoint

## Verboden
- Geen `any` types in TypeScript
- Geen business logic in controllers/endpoints
- Geen direct gebruik van DbContext buiten Infrastructure laag
- Geen hardcoded strings voor configuratie
- Commit nooit zonder dat tests slagen
```

### 1.3 — Claude Code Custom Commands

**`.claude/commands/implement-feature.md`**
```markdown
Implementeer de feature beschreven in: docs/specs/$ARGUMENTS.md

Stappen:
1. Lees de spec volledig door
2. Identificeer welke domain entities nodig zijn
3. Implementeer in deze volgorde:
   a. Domain entities/value objects
   b. EF Core configuraties + migratie
   c. MediatR commands/queries + validators
   d. API endpoints
   e. Angular service + store
   f. Angular components + routing
4. Schrijf unit tests voor alle handlers
5. Schrijf integration tests voor API endpoints
6. Voer alle tests uit en fix eventuele fouten
7. Maak een commit met conventional commit message
```

**`.claude/commands/write-tests.md`**
```markdown
Schrijf tests voor: $ARGUMENTS

1. Analyseer de bestaande code
2. Schrijf unit tests (xUnit + FluentAssertions + NSubstitute)
3. Schrijf integration tests met WebApplicationFactory
4. Zorg voor edge cases en foutscenario's
5. Voer alle tests uit
```

### 1.4 — Linting & Formatting

**Backend (.NET):**
- `.editorconfig` met C# conventies
- `dotnet format` als CI check
- Optioneel: Roslynator of SonarAnalyzer NuGet packages

**Frontend (Angular/Nx):**
- ESLint met `@angular-eslint` en `@nx/eslint`
- Prettier voor formatting
- `lint-staged` + `husky` voor pre-commit hooks (optioneel, Claude Code runt toch tests)

### 1.5 — CI/CD Basis

Een minimale GitHub Actions workflow die bij elke PR:
1. Backend: `dotnet build` → `dotnet test` → `dotnet format --verify-no-changes`
2. Frontend: `nx lint` → `nx test` → `nx build`

---

## Fase 2: Feature Specificaties

### 2.1 — Spec Template

Maak `docs/specs/_template.md`:

```markdown
# Feature: [Naam]

## Context
Beschrijving van waarom deze feature nodig is.

## User Stories
- Als [rol] wil ik [actie] zodat [waarde]

## Acceptance Criteria
- [ ] Criterium 1
- [ ] Criterium 2

## Domain Model
Welke entities, value objects, en relaties zijn betrokken?

## API Endpoints
| Method | Route | Beschrijving |
|--------|-------|-------------|
| POST   | /api/v1/... | ... |

## Business Rules
- Regel 1
- Regel 2

## UI/UX
Beschrijving van de schermen, eventueel wireframes als afbeelding.

## Events (async)
Welke events worden gepubliceerd naar RabbitMQ?

## Out of Scope
Wat hoort NIET bij deze feature?
```

### 2.2 — Feature Roadmap (volgorde van implementatie)

De volgorde is cruciaal. Begin met de kern en bouw uit:

| # | Feature | Waarom deze volgorde |
|---|---------|---------------------|
| 1 | **Tenant & Auth Setup** | Fundament: multi-tenancy + authenticatie |
| 2 | **Staff Management** | Wie werkt er? Nodig voor alles wat volgt |
| 3 | **Service Catalog** | Wat biedt de salon aan? (knippen, kleuren, etc.) |
| 4 | **Client Management** | Wie zijn de klanten? |
| 5 | **Appointment Booking** | De kernfunctie — alles komt hier samen |
| 6 | **Calendar View** | Visuele weergave van afspraken (dag/week/maand) |
| 7 | **Notifications** | Afspraakbevestigingen, herinneringen (RabbitMQ + email/SMS) |
| 8 | **Billing & Invoicing** | Facturering na afspraken |
| 9 | **Dashboard & Reporting** | Omzet, bezettingsgraad, populaire diensten |
| 10 | **Client Portal** | Klanten kunnen zelf boeken (public-facing) |

### 2.3 — Voorbeeld Spec: Appointment Booking

```markdown
# Feature: Appointment Booking

## Context
Het boeken van afspraken is de kernfunctionaliteit van de salon software.
Een afspraak koppelt een klant aan een medewerker voor een of meerdere
diensten op een specifiek tijdstip.

## User Stories
- Als salon eigenaar wil ik een afspraak inplannen voor een klant
  zodat het schema van mijn medewerkers up-to-date is.
- Als medewerker wil ik mijn eigen agenda zien
  zodat ik weet welke klanten ik vandaag heb.

## Acceptance Criteria
- [ ] Een afspraak heeft: klant, medewerker, 1+ diensten, starttijd
- [ ] De duur wordt automatisch berekend op basis van de geselecteerde diensten
- [ ] Het systeem voorkomt dubbele boekingen voor dezelfde medewerker
- [ ] Een afspraak kan de status hebben: Scheduled, InProgress, Completed, Cancelled, NoShow
- [ ] Bij het aanmaken wordt een AppointmentCreated event gepubliceerd

## Domain Model
- Appointment (aggregate root)
  - Id, TenantId, ClientId, StaffMemberId, StartTime, EndTime, Status, Notes
- AppointmentService (value object binnen Appointment)
  - ServiceId, ServiceName, Duration, Price

## API Endpoints
| Method | Route | Beschrijving |
|--------|-------|-------------|
| POST   | /api/v1/appointments | Maak afspraak |
| GET    | /api/v1/appointments?date=&staffId= | Lijst ophalen |
| GET    | /api/v1/appointments/{id} | Detail ophalen |
| PUT    | /api/v1/appointments/{id} | Afspraak wijzigen |
| PATCH  | /api/v1/appointments/{id}/status | Status wijzigen |
| DELETE | /api/v1/appointments/{id} | Afspraak annuleren |

## Business Rules
- Een afspraak kan niet in het verleden worden gepland
- Een medewerker kan niet dubbel geboekt worden (overlap check)
- Alleen diensten die de medewerker beheerst mogen worden gekoppeld
- Annuleren is alleen mogelijk als status = Scheduled
- Minimale afspraakduur: 15 minuten

## Events
- AppointmentCreated → trigger bevestigingsmail
- AppointmentCancelled → trigger annuleringsmail
- AppointmentStatusChanged → update dashboard stats

## Out of Scope
- Online boeken door klanten (dat is de Client Portal feature)
- Betalingsafhandeling (dat is Billing)
```

---

## Fase 3: Ontwikkelworkflow met Claude Code

### 3.1 — Dagelijkse Workflow

```
Jij (Architect)                    Claude Code (Developer)
─────────────                      ──────────────────────
Schrijf/review spec          →
                              ←    "Spec gelezen, ik ga beginnen
                                    met de domain entities..."
                              ←    Implementeert stap voor stap
Review code, geef feedback   →
                              ←    Past aan op basis van feedback
Approve & merge              →
Schrijf volgende spec        →     ... herhaal ...
```

### 3.2 — Hoe je Claude Code aanstuurt

**Feature implementeren:**
```bash
claude "/implement-feature appointment-booking"
```

**Specifieke taak:**
```bash
claude "Voeg een overlap-check toe aan de CreateAppointmentCommandHandler.
        Als een medewerker al een afspraak heeft die overlapt met de
        gevraagde tijd, return een error. Schrijf ook een unit test."
```

**Code review laten doen:**
```bash
claude "Review de code in Application/Features/Appointments/ en check of
        het voldoet aan de conventies in CLAUDE.md. Geef suggesties."
```

**Bug fixen:**
```bash
claude "De integration test voor POST /api/v1/appointments faalt met een
        409 conflict. De overlap check lijkt te streng. Debug dit."
```

### 3.3 — Tips voor effectief werken met Claude Code

1. **Eén feature per sessie.** Start een verse sessie per feature voor een schone context.

2. **Laat Claude Code zijn eigen werk verifiëren.** Eindig instructies altijd met "voer de tests uit en fix fouten".

3. **Gebruik git branches.** Laat Claude Code per feature op een branch werken:
   ```bash
   claude "Maak een branch feat/appointment-booking aan en werk daarop."
   ```

4. **Wees specifiek over wat je NIET wilt.** Claude Code is enthousiast — zonder grenzen bouwt hij teveel. Gebruik "Out of Scope" in je specs.

5. **Itereer in kleine stappen.** Liever 5 kleine prompts dan 1 enorme. Je houdt meer controle en kunt bijsturen.

6. **Review altijd de migraties.** EF Core migraties zijn moeilijk terug te draaien. Bekijk ze handmatig voordat je ze toepast.

---

## Fase 4: Deployment

### 4.1 — Omgevingen

| Omgeving | Doel | Infra |
|----------|------|-------|
| Local | Development | .NET Aspire + Docker Compose |
| Dev/Test | Integration testing | Docker op een VPS of Azure Container Apps |
| Staging | Pre-productie, demo's | Identiek aan productie |
| Production | Live | Azure Container Apps / AWS ECS / DigitalOcean |

### 4.2 — Deployment Strategie

**Stap 1: Containerize**
- Dockerfile per service (API)
- Docker Compose voor lokale stack (PostgreSQL, RabbitMQ, API, Angular)
- .NET Aspire genereert manifest voor container orchestratie

**Stap 2: CI/CD Pipeline (GitHub Actions)**
```
Push to main → Build → Test → Docker build → Push to registry → Deploy
```

**Stap 3: Infrastructure as Code**
- Pulumi (C#) of Terraform voor cloud resources
- Database migraties als onderdeel van deployment pipeline

### 4.3 — Laat Claude Code helpen met deployment

```bash
claude "Maak een Dockerfile voor de API die multi-stage build gebruikt.
        Maak ook een docker-compose.yml die de API, PostgreSQL, en
        RabbitMQ opstart. Zorg dat .NET Aspire dashboard beschikbaar is."
```

```bash
claude "Maak een GitHub Actions workflow die bij push naar main:
        1. Backend build + test
        2. Frontend build + test
        3. Docker image bouwt en pusht naar GitHub Container Registry
        4. Deploy naar de staging omgeving via SSH"
```

---

## Fase 5: Kwaliteitsbewaking

### 5.1 — Wat je zelf moet doen (niet delegeren aan AI)

- **Architectuurbeslissingen** reviewen en vastleggen in ADRs
- **Database migraties** handmatig controleren
- **Security** configuratie (auth, CORS, rate limiting)
- **Feature specs** schrijven en goedkeuren
- **Code reviews** — Claude Code schrijft, jij reviewt

### 5.2 — Wat Claude Code kan doen

- Code implementeren volgens specs
- Tests schrijven en uitvoeren
- Refactoring en code cleanup
- Boilerplate genereren (CRUD, nieuwe features scaffolden)
- Documentation schrijven
- CI/CD pipelines opzetten
- Docker configuratie

### 5.3 — Definition of Done (per feature)

- [ ] Spec is volledig geïmplementeerd (alle acceptance criteria)
- [ ] Unit tests geschreven en groen
- [ ] Integration tests geschreven en groen
- [ ] Geen linting errors
- [ ] Code reviewed door jou
- [ ] Conventional commit message
- [ ] Feature branch gemerged naar main

---

## Quick Start Checklist

### Week 1: Fundament
- [ ] Maak GitHub repo aan
- [ ] Schrijf `docs/domain-model.md`
- [ ] Schrijf eerste 3 ADRs
- [ ] Maak `CLAUDE.md` en `.claude/` folder aan
- [ ] Setup Nx workspace + Angular app
- [ ] Setup .NET solution met Clean Architecture projecten
- [ ] Setup .NET Aspire AppHost
- [ ] Configureer ESLint, Prettier, EditorConfig
- [ ] Maak basis CI workflow in GitHub Actions
- [ ] Schrijf spec voor Feature 1 (Tenant & Auth)

### Week 2-3: Eerste features
- [ ] Implementeer Tenant & Auth met Claude Code
- [ ] Implementeer Staff Management
- [ ] Implementeer Service Catalog

### Week 4-5: Kernfunctionaliteit
- [ ] Implementeer Client Management
- [ ] Implementeer Appointment Booking
- [ ] Implementeer Calendar View

### Week 6+: Uitbreiding
- [ ] Notifications (RabbitMQ integration)
- [ ] Billing
- [ ] Dashboard
- [ ] Client Portal
