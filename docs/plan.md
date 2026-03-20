# AI-First Development Plan: Chairly — Salon Management SaaS

## Overview

This plan describes a structured approach to building a Salon Management SaaS product using Claude Code as the primary development tool. The architect/product owner defines features interactively with Claude Code, which orchestrates specialized agents for implementation, review, and quality assurance.

---

## Phase 0: Project Foundation (Interactive)

> **Goal:** Define the domain, architecture, and technical decisions. Done interactively with Claude Code.

### 0.1 — Product Discovery & Domain Model

Create `docs/domain-model.md` with:
- **Bounded Contexts** (DDD-style): Bookings, Clients, Staff, Services, Billing, Notifications
- **Core Entities** per context with relationships
- **Ubiquitous Language**: a glossary for consistent terminology
- **User Roles**: Owner, Manager, Staff Member

### 0.2 — Architecture Decision Records (ADRs)

Create `docs/adr/` with technical decisions:

| ADR | Decision |
|-----|----------|
| ADR-001 | Monorepo with Nx |
| ADR-002 | .NET Aspire for local dev orchestration |
| ADR-003 | PostgreSQL with EF Core (code-first, database-per-tenant) |
| ADR-004 | RabbitMQ for async events |
| ADR-005 | Custom mediator (MediatR pattern, no package) |
| ADR-006 | Angular with standalone components, signals, NgRx SignalStore, Sheriff |
| ADR-007 | Multi-tenancy (database-per-tenant) |
| ADR-008 | Keycloak for authentication |
| ADR-009 | Timestamps instead of status columns |
| ADR-010 | AI-first development with Claude Code |

---

## Phase 1: Repository & Tooling Setup (Interactive)

> **Goal:** Set up the repository structure, CI/CD, and development tooling.

### 1.1 — Monorepo Structure

```
chairly/
├── docs/                    # Domain model, ADRs, specs
├── src/
│   ├── backend/             # .NET solution
│   │   ├── Chairly.AppHost/ # .NET Aspire host
│   │   ├── Chairly.Api/     # VSA slices
│   │   ├── Chairly.Domain/  # Entities, value objects
│   │   ├── Chairly.Infrastructure/ # EF Core, RabbitMQ, Keycloak
│   │   └── Chairly.Tests/   # Unit + integration tests
│   └── frontend/
│       └── chairly/         # Nx Angular workspace
├── .claude/
│   ├── agents/              # Agent definitions
│   ├── skills/              # Skill orchestrators + pattern references
│   └── tasks/               # Feature specs + task lists
└── CLAUDE.md                # Root instructions
```

### 1.2 — CI/CD

GitHub Actions workflow on every PR:
1. Backend: `dotnet build` -> `dotnet test` -> `dotnet format --verify-no-changes`
2. Frontend: `nx lint` -> `nx test` -> `nx build`

---

## Phase 2: Feature Development

> **Goal:** Iteratively implement features using the Claude Code agent workflow.

### 2.1 — Workflow

```
1. /create-spec {feature} [--issue N]    → Interactive spec creation + review + PR
2. Human reviews spec PR                  → Merge to main when approved
3. /implement {feature}                   → Parallel backend/frontend agents + review + QA + PR
4. Human reviews code PR                  → Merge to main when approved
5. /rework-spec {PR#} or /rework-code {PR#}  → If changes needed
```

### 2.2 — Feature Roadmap (Implementation Order)

| # | Feature | Why this order |
|---|---------|----------------|
| 1 | **Tenant & Auth Setup** | Foundation: multi-tenancy + authentication |
| 2 | **Staff Management** | Who works there? Needed for everything that follows |
| 3 | **Service Catalog** | What does the salon offer? |
| 4 | **Client Management** | Who are the clients? |
| 5 | **Booking Management** | Core feature — everything comes together here |
| 6 | **Calendar View** | Visual representation of bookings |
| 7 | **Notifications** | Booking confirmations, reminders (RabbitMQ + email) |
| 8 | **Billing & Invoicing** | Invoicing after bookings |
| 9 | **Dashboard & Reporting** | Revenue, occupancy, popular services |
| 10 | **Client Portal** | Clients can book themselves (public-facing) |

---

## Phase 3: Deployment

### Environments

| Environment | Purpose | Infra |
|-------------|---------|-------|
| Local | Development | .NET Aspire + Docker Compose |
| Dev/Test | Integration testing | Containerized |
| Staging | Pre-production | Identical to production |
| Production | Live | Azure Container Apps / similar |

### Deployment Strategy

1. Containerize (Dockerfile per service, Docker Compose for local)
2. CI/CD pipeline: Push -> Build -> Test -> Docker build -> Deploy
3. Infrastructure as Code for cloud resources

---

## Phase 4: Quality Assurance

### Architect Responsibilities (Do Not Delegate)

- Architecture decisions — review and record in ADRs
- Database migrations — manually verify before applying
- Security configuration (auth, CORS, rate limiting)
- Feature specs — write and approve via `/create-spec`
- Code reviews — agents write, architect reviews PRs

### Automated Quality (Agent Team)

- Backend QA: build, test, format
- Frontend QA: lint, format, test, build, e2e
- Automated code review by reviewer agents
- CI/CD pipeline on every PR

### Definition of Done (Per Feature)

- [ ] Spec reviewed and merged to main
- [ ] All tasks implemented
- [ ] Backend QA passes
- [ ] Frontend QA passes
- [ ] Code reviewed by architect
- [ ] CI green
- [ ] Feature branch merged to main
