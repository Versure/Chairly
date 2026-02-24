# AI-First Development Plan: Chairly — Salon Management SaaS

## Overview

This plan describes a structured approach to building a Salon Management SaaS product using Claude Code as the primary development tool. The approach has two distinct modes:

- **Phase 0–1 (Architecture & Setup):** You are the architect and product owner. Claude Code is your senior developer in **interactive mode** — it asks questions, you make decisions.
- **Phase 2–5 (Implementation & Beyond):** [Ralph](https://github.com/snarktank/ralph) runs Claude Code **autonomously** in a loop, implementing features from PRDs without blocking your workflow. Ralph operates in WSL via a separate git clone, while you continue working in your Windows environment.

---

## Phase 0: Project Foundation (Interactive — No Ralph)

> **Goal:** Define the domain, architecture, and technical decisions. This is done interactively with Claude Code — Ralph is NOT used in this phase.

### 0.1 — Product Discovery & Domain Model

Before Claude Code writes a single line of code, define the domain. This is the most important document in the entire project.

Create `docs/domain-model.md` with:

- **Bounded Contexts** (DDD-style): e.g. `Appointments`, `Clients`, `Staff`, `Services`, `Billing`, `Notifications`
- **Core Entities** per context with their relationships
- **Ubiquitous Language**: a glossary so Claude Code uses consistent terminology (e.g. "Appointment" vs "Booking" — pick one)
- **User Roles**: Owner, Staff Member, Client (if building a client portal)

### 0.2 — Architecture Decision Records (ADRs)

Create a `docs/adr/` folder. Document technical decisions so Claude Code (and Ralph) can reference them:

| ADR | Decision |
|-----|----------|
| ADR-001 | Monorepo with Nx |
| ADR-002 | .NET Aspire as orchestrator for local dev |
| ADR-003 | PostgreSQL with EF Core (code-first migrations) |
| ADR-004 | RabbitMQ for async events (e.g. appointment reminders) |
| ADR-005 | CQRS-light: MediatR for commands/queries, no event sourcing |
| ADR-006 | Angular with standalone components, signals, NgRx SignalStore |
| ADR-007 | Multi-tenancy strategy (schema-per-tenant vs row-level) |
| ADR-008 | Auth strategy (e.g. Keycloak, Auth0, or ASP.NET Identity) |

---

## Phase 1: Repository & Tooling Setup (Interactive — No Ralph)

> **Goal:** Set up the repository structure, CI/CD, and development tooling. Done interactively with Claude Code.

### 1.1 — Monorepo Structure

```
chairly/
├── docs/
│   ├── domain-model.md
│   ├── adr/
│   └── specs/                          # Feature specifications
│       ├── _template.md
│       ├── appointment-booking.md
│       └── client-management.md
├── scripts/
│   └── ralph/                          # Ralph autonomous agent
│       ├── ralph.sh                    # The agent loop script
│       ├── CLAUDE.md                   # Ralph's prompt template
│       ├── prd.json                    # Current feature tasks (per run)
│       ├── progress.txt                # Memory between iterations
│       └── archive/                    # Previous Ralph runs
├── src/
│   ├── backend/
│   │   ├── Chairly.sln
│   │   ├── Chairly.AppHost/           # .NET Aspire host
│   │   ├── Chairly.ServiceDefaults/   # Shared Aspire config
│   │   ├── Chairly.Api/               # WebAPI project
│   │   ├── Chairly.Application/       # CQRS handlers, services
│   │   ├── Chairly.Domain/            # Entities, value objects
│   │   ├── Chairly.Infrastructure/    # EF Core, RabbitMQ, etc.
│   │   └── Chairly.Tests/             # Unit + integration tests
│   └── frontend/
│       └── chairly-app/               # Nx Angular workspace
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
└── CLAUDE.md                           # Root-level Claude instructions
```

### 1.2 — CLAUDE.md (Root Instructions)

This is the most important file for the AI-first workflow. Claude Code reads it automatically in both interactive and autonomous (Ralph) mode. It must support both modes.

```markdown
# CLAUDE.md

## Project
Chairly - Multi-tenant SaaS platform for salons and barbershops.

## Architecture
- Read `docs/domain-model.md` for the domain model
- Read `docs/adr/` for architecture decisions
- Feature specs are in `docs/specs/`

## Tech Stack
- Backend: .NET 9, ASP.NET Core Web API, EF Core, MediatR
- Frontend: Angular 19, Nx, NgRx SignalStore, Tailwind CSS
- Infra: PostgreSQL, RabbitMQ, .NET Aspire, Docker

## Code Conventions — Backend
- Follow Clean Architecture: Domain → Application → Infrastructure → Api
- Use MediatR for all commands and queries
- Commands: `Create{Entity}Command`, `Update{Entity}Command`, `Delete{Entity}Command`
- Queries: `Get{Entity}Query`, `Get{Entities}ListQuery`
- Handlers always in `Application/Features/{Context}/{Commands|Queries}/`
- Use FluentValidation for input validation
- Entity configurations in separate `IEntityTypeConfiguration<T>` classes
- All endpoints via Minimal APIs, grouped per feature in extension methods
- Use Result pattern (no exceptions for business logic)
- Test coverage: Unit tests for handlers, integration tests for API endpoints

## Code Conventions — Frontend
- Standalone components, no NgModules
- Use Angular signals and NgRx SignalStore for state
- Smart/Dumb component pattern: containers load data, presentational components display it
- Services for API calls, one service per backend context
- Use Tailwind CSS, no custom SCSS unless absolutely necessary
- Reactive forms with typed FormGroups
- Lazy-loaded routes per feature module

## Working Method — Interactive Mode
When working with a human developer interactively:
- STOP and ask questions when something is not described in a spec, ADR, or previous instruction
- Provide 2-3 concrete options with pros and cons
- Wait for the human's choice before proceeding

## Working Method — Autonomous/Headless Mode (Ralph)
When running autonomously via Ralph or in headless mode:
- Do NOT stop to ask questions — there is no one to answer
- Make decisions based on existing patterns in the codebase, ADRs, and specs
- Follow conventions established in docs/ and existing code
- Document any significant decisions in progress.txt
- When in doubt, choose the simplest approach that follows existing patterns

## Implementation Order
1. Always read the relevant spec in `docs/specs/` before starting
2. Create domain entities and value objects first
3. Then EF Core configuration and migration
4. Then MediatR handlers with FluentValidation
5. Then API endpoints
6. Then Angular feature (service → store → components → routes)
7. Write tests at every step
8. Commit with conventional commits: feat(appointments): add booking endpoint

## Forbidden
- No `any` types in TypeScript
- No business logic in controllers/endpoints
- No direct use of DbContext outside Infrastructure layer
- No hardcoded strings for configuration
- Never commit without tests passing
```

### 1.3 — Linting & Formatting

**Backend (.NET):**
- `.editorconfig` with C# conventions
- `dotnet format` as CI check
- Optional: Roslynator or SonarAnalyzer NuGet packages

**Frontend (Angular/Nx):**
- ESLint with `@angular-eslint` and `@nx/eslint`
- Prettier for formatting

### 1.4 — CI/CD Basics

A minimal GitHub Actions workflow that runs on every PR:
1. Backend: `dotnet build` → `dotnet test` → `dotnet format --verify-no-changes`
2. Frontend: `nx lint` → `nx test` → `nx build`

---

## Phase 2: Ralph Setup (One-Time Configuration)

> **Goal:** Set up Ralph in WSL so it can autonomously implement features on a separate clone, without blocking your Windows workflow.

### 2.1 — WSL Environment

Ralph runs in WSL Ubuntu with its own clone of the repository:

```
Windows (your interactive work):
  c:\Projects\Prive\Chairly\Chairly\     ← you work here

WSL (Ralph autonomous agent):
  ~/projects/Chairly/                      ← Ralph's base clone
```

**Prerequisites in WSL:**
- Git
- Node.js (for Claude Code CLI)
- Claude Code CLI (`npm install -g @anthropic-ai/claude-code`)
- GitHub CLI (`gh`) — authenticated with SSH
- `jq` (`sudo apt install jq`)
- SSH key configured and added to GitHub

### 2.2 — Install Ralph

```bash
cd ~/projects
git clone https://github.com/snarktank/ralph.git

# Copy Ralph files into the Chairly project
cd ~/projects/Chairly
mkdir -p scripts/ralph
cp ~/projects/ralph/ralph.sh scripts/ralph/
cp ~/projects/ralph/CLAUDE.md scripts/ralph/CLAUDE.md
chmod +x scripts/ralph/ralph.sh
```

### 2.3 — Ralph Configuration

Ralph expects all its files in the same directory as `ralph.sh`:

```
scripts/ralph/
├── ralph.sh          # The loop script
├── CLAUDE.md         # Prompt template (Ralph's instructions to Claude Code)
├── prd.json          # Current feature tasks (created per feature)
├── progress.txt      # Append-only memory between iterations
├── .last-branch      # Branch tracking
└── archive/          # Previous runs (auto-archived)
```

Key settings in `ralph.sh`:
- Tool: `claude` (not amp)
- Max iterations: configurable per run (default 10)

### 2.4 — Verify Ralph Setup

Create a hello world PRD and run Ralph to verify the entire chain works:

```bash
cd ~/projects/Chairly

# Create a minimal prd.json for testing
cat > scripts/ralph/prd.json << 'EOF'
{
  "project": "Chairly",
  "branchName": "ralph/hello-world",
  "description": "Hello World - Verify Ralph setup",
  "userStories": [
    {
      "id": "HW-001",
      "title": "Create README.md",
      "description": "Create a README.md with the project name and description.",
      "acceptanceCriteria": [
        "README.md exists in project root",
        "Contains project name 'Chairly'",
        "Contains description 'AI-first salon management platform'"
      ],
      "priority": 1,
      "passes": false,
      "notes": ""
    }
  ]
}
EOF

# Run Ralph with Claude Code
./scripts/ralph/ralph.sh --tool claude 3
```

---

## Phase 3: Feature Development with Ralph

> **Goal:** Define features as specs, convert them to PRDs, and let Ralph implement them autonomously.

### 3.1 — Feature Spec → PRD Workflow

```
You (Architect)                         Ralph (Autonomous Agent in WSL)
───────────────                         ───────────────────────────────
1. Write spec in docs/specs/
2. Convert spec → prd.json
   (small, focused user stories)
3. Place prd.json in scripts/ralph/
4. Start Ralph in WSL              →    5. Reads prd.json
                                        6. Picks highest priority story
                                        7. Implements it (fresh Claude instance)
                                        8. Runs quality checks
                                        9. Commits if passing
                                       10. Updates prd.json (passes: true)
                                       11. Logs progress to progress.txt
                                       12. Repeats until all stories done
5. Review branch on GitHub
6. Merge or request changes
```

### 3.2 — Writing Effective PRDs for Ralph

Each user story must be **small enough to complete in one Claude Code context window**. This is critical — stories that are too large produce poor code.

**Right-sized stories:**
- Add a database entity and migration
- Add a UI component to an existing page
- Create a MediatR command handler with validation
- Add an API endpoint for an existing handler
- Add a filter dropdown to a list

**Too big (split these):**
- "Build the entire dashboard"
- "Add authentication"
- "Refactor the API"
- "Implement appointment booking" (split into entity, handler, endpoint, UI stories)

### 3.3 — Spec Template

Create `docs/specs/_template.md`:

```markdown
# Feature: [Name]

## Context
Description of why this feature is needed.

## User Stories
- As a [role] I want [action] so that [value]

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

## Domain Model
Which entities, value objects, and relationships are involved?

## API Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST   | /api/v1/... | ... |

## Business Rules
- Rule 1
- Rule 2

## UI/UX
Description of screens, wireframes if applicable.

## Events (async)
Events published to RabbitMQ.

## Out of Scope
What does NOT belong to this feature?
```

### 3.4 — PRD JSON Format

Convert each spec into a `prd.json` with granular stories:

```json
{
  "project": "Chairly",
  "branchName": "ralph/appointment-booking",
  "description": "Appointment Booking - Core scheduling functionality",
  "userStories": [
    {
      "id": "AB-001",
      "title": "Create Appointment entity and migration",
      "description": "Add Appointment aggregate root with EF Core configuration.",
      "acceptanceCriteria": [
        "Appointment entity with Id, TenantId, ClientId, StaffMemberId, StartTime, EndTime, Status, Notes",
        "AppointmentService value object with ServiceId, ServiceName, Duration, Price",
        "EF Core configuration in separate IEntityTypeConfiguration class",
        "Migration generated and runs successfully",
        "dotnet build passes"
      ],
      "priority": 1,
      "passes": false,
      "notes": ""
    },
    {
      "id": "AB-002",
      "title": "Create booking command handler",
      "description": "MediatR handler for creating appointments with overlap validation.",
      "acceptanceCriteria": [
        "CreateAppointmentCommand with FluentValidation",
        "Handler checks for staff member time overlap",
        "Returns Result pattern (not exceptions)",
        "Unit tests for handler including overlap scenario",
        "dotnet test passes"
      ],
      "priority": 2,
      "passes": false,
      "notes": "Depends on AB-001"
    }
  ]
}
```

### 3.5 — Running Ralph

```bash
# SSH into WSL
wsl

# Navigate to project
cd ~/projects/Chairly

# Pull latest changes (in case you pushed from Windows)
git pull

# Start Ralph (default 10 iterations, using Claude Code)
./scripts/ralph/ralph.sh --tool claude

# Or with custom iteration count
./scripts/ralph/ralph.sh --tool claude 20

# Monitor progress
cat scripts/ralph/progress.txt
cat scripts/ralph/prd.json | jq '.userStories[] | {id, title, passes}'
```

### 3.6 — Feature Roadmap (Implementation Order)

| # | Feature | Why this order |
|---|---------|----------------|
| 1 | **Tenant & Auth Setup** | Foundation: multi-tenancy + authentication |
| 2 | **Staff Management** | Who works there? Needed for everything that follows |
| 3 | **Service Catalog** | What does the salon offer? (haircuts, coloring, etc.) |
| 4 | **Client Management** | Who are the clients? |
| 5 | **Appointment Booking** | The core feature — everything comes together here |
| 6 | **Calendar View** | Visual representation of appointments (day/week/month) |
| 7 | **Notifications** | Appointment confirmations, reminders (RabbitMQ + email/SMS) |
| 8 | **Billing & Invoicing** | Invoicing after appointments |
| 9 | **Dashboard & Reporting** | Revenue, occupancy rate, popular services |
| 10 | **Client Portal** | Clients can book themselves (public-facing) |

Each feature follows the flow: **write spec** → **convert to prd.json** → **run Ralph** → **review & merge**.

---

## Phase 4: Deployment

### 4.1 — Environments

| Environment | Purpose | Infra |
|-------------|---------|-------|
| Local | Development | .NET Aspire + Docker Compose |
| Dev/Test | Integration testing | Docker on a VPS or Azure Container Apps |
| Staging | Pre-production, demos | Identical to production |
| Production | Live | Azure Container Apps / AWS ECS / DigitalOcean |

### 4.2 — Deployment Strategy

**Step 1: Containerize**
- Dockerfile per service (API)
- Docker Compose for local stack (PostgreSQL, RabbitMQ, API, Angular)
- .NET Aspire generates manifest for container orchestration

**Step 2: CI/CD Pipeline (GitHub Actions)**
```
Push to main → Build → Test → Docker build → Push to registry → Deploy
```

**Step 3: Infrastructure as Code**
- Pulumi (C#) or Terraform for cloud resources
- Database migrations as part of deployment pipeline

---

## Phase 5: Quality Assurance

### 5.1 — Your Responsibilities (Do Not Delegate to AI)

- **Architecture decisions** — review and record in ADRs
- **Database migrations** — manually verify before applying
- **Security** configuration (auth, CORS, rate limiting)
- **Feature specs** — write and approve
- **Code reviews** — Ralph/Claude Code writes, you review

### 5.2 — Ralph's Automated Quality Checks

Ralph runs quality checks after every story iteration:
- `dotnet build` — compilation check
- `dotnet test` — unit + integration tests
- `dotnet format --verify-no-changes` — formatting
- `nx lint` — frontend linting
- `nx test` — frontend tests

If checks fail, Ralph will attempt to fix the issues in the same iteration before committing.

### 5.3 — Definition of Done (Per Feature)

- [ ] All user stories in prd.json have `passes: true`
- [ ] Ralph's quality checks all pass
- [ ] Code reviewed by you
- [ ] Conventional commit messages
- [ ] Feature branch merged to main
- [ ] progress.txt documents learnings

---

## Quick Start Checklist

### Week 1: Foundation (Interactive with Claude Code, No Ralph)
- [ ] Create GitHub repo
- [ ] Write `docs/domain-model.md`
- [ ] Write first 3 ADRs
- [ ] Create `CLAUDE.md` (dual-mode: interactive + autonomous)
- [ ] Setup Nx workspace + Angular app
- [ ] Setup .NET solution with Clean Architecture projects
- [ ] Setup .NET Aspire AppHost
- [ ] Configure ESLint, Prettier, EditorConfig
- [ ] Create basic CI workflow in GitHub Actions
- [ ] Push initial codebase to GitHub

### Week 2: Ralph Setup
- [ ] Configure WSL environment (git, node, claude, gh, jq, ssh)
- [ ] Clone repo in WSL (`~/projects/Chairly`)
- [ ] Install Ralph into `scripts/ralph/`
- [ ] Run hello world test with Ralph
- [ ] Write spec for Feature 1 (Tenant & Auth)

### Week 3–4: First Features (Ralph)
- [ ] Convert Tenant & Auth spec → prd.json → run Ralph → review & merge
- [ ] Convert Staff Management spec → prd.json → run Ralph → review & merge
- [ ] Convert Service Catalog spec → prd.json → run Ralph → review & merge

### Week 5–6: Core Functionality (Ralph)
- [ ] Client Management
- [ ] Appointment Booking
- [ ] Calendar View

### Week 7+: Extensions (Ralph)
- [ ] Notifications (RabbitMQ integration)
- [ ] Billing
- [ ] Dashboard
- [ ] Client Portal
