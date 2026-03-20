# Staff create email alignment

## Overview

Issue #73 meldt dat bij **Medewerker toevoegen** een fout ontstaat omdat `Email` verplicht is in de backend, terwijl het veld ontbreekt in de frontend dialoog. Deze feature herstelt contract-alignement in de **Staff** bounded context door het bestaande create-staff endpoint te hergebruiken, e-mail expliciet verplicht te maken in de UI, en validatiegedrag consistent te maken tussen inline formulierfeedback en API-foutmapping.

## Domain Context

- Bounded context: Staff
- Key entities involved: StaffMember
- Ubiquitous language: Staff Member, Owner, Manager (see docs/domain-model.md)

## User Stories

- Als **Owner** wil ik een medewerker kunnen toevoegen met een verplicht e-mailadres, zodat provisioning naar identity correct verloopt.
- Als **Manager** wil ik dezelfde create-staff flow kunnen gebruiken als Owner, zodat operationeel personeelsbeheer mogelijk blijft.
- Als **gebruiker** wil ik directe inline validatiefouten én duidelijke serverfouten zien, zodat ik invoerproblemen snel kan corrigeren.
- Als **developer** wil ik dat frontend requestmodellen en backend commandvalidatie hetzelfde contract hanteren, zodat regressies op verplichte velden worden voorkomen.

## Backend Tasks

### B1 — Bevestig en documenteer create-staff validatiecontract met verplicht e-mailadres

- Bestaande **StaffMember** blijft leidend met bestaand attribuut `Email`.
- Bevestig dat `CreateStaffMemberCommand` validatie `[Required]`, `[EmailAddress]`, `[MaxLength(256)]` op `Email` heeft.
- Endpoint: `POST /api/staff` (hergebruik, geen nieuw endpoint).
- Autorisatie: bestaande `RequireManager` schrijfpolicy.

### B2 — Voeg backend regressietests toe voor e-mailvalidatie en autorisatieroute

- Unit tests voor `CreateStaffMemberCommand` validatie: ontbrekende e-mail, ongeldig e-mailformaat.
- Handler/endpoint regressietests: geldige e-mail happy flow, policygedrag (Owner/Manager toegestaan, niet-geautoriseerd geblokkeerd).
- Waarborg dat bestaand Keycloak-foutgedrag ongewijzigd blijft.

## Frontend Tasks

### F1 — Voeg verplicht e-mailadres toe aan medewerker-toevoegen dialoog en requestmodel

- In `staff-form-dialog` wordt een nieuw invoerveld toegevoegd:
  - Label: **E-mailadres**
  - Type: email
  - Placeholder: `naam@salon.nl` (of vergelijkbaar)
- Inline validatie (Nederlands):
  - verplicht: "E-mailadres is verplicht."
  - formaat: "Voer een geldig e-mailadres in."
  - max lengte: passende melding volgens bestaand patroon.
- Submit blijft disabled bij ongeldig formulier.
- Frontend `CreateStaffMemberRequest` bevat `email` en verstuurt dit naar `POST /api/staff`.

### F2 — Implementeer inline validatie plus API-foutmapping voor create-staff flow

- API-foutmapping:
  - servervalidatie op `email` koppelen aan formulierfout en/of algemene foutmelding.
  - foutteksten blijven Nederlands en consistent met bestaande staff UX.
- Component tests voor `staff-form-dialog`: e-mailveld aanwezig, required + format validators, submit gating.
- Service/store tests: `email` wordt meegestuurd in create request en API-validatiefouten worden correct gemapt.
- E2E/flowtests voor staff aanmaken met Owner en Manager.

## Acceptance Criteria

- [ ] De medewerker-toevoegen dialoog bevat een verplicht veld **E-mailadres** met Nederlandse label- en foutteksten.
- [ ] Frontend `CreateStaffMemberRequest` bevat `email` en verstuurt dit naar `POST /api/staff`.
- [ ] Het bestaande backend endpoint en commandflow worden hergebruikt (geen nieuw create endpoint).
- [ ] Bij lege of ongeldig geformatteerde e-mail toont de UI inline validatiefouten vóór submit.
- [ ] Bij backend-validatiefouten (400/422-achtige responsevorm) toont de UI een bruikbare Nederlandse foutmelding op formulierniveau.
- [ ] Owner en Manager kunnen staff aanmaken; ongeautoriseerde rollen krijgen consistente autorisatiefout volgens bestaande policies.
- [ ] Bestaande staff update/deactivate/reactivate flows blijven ongewijzigd.
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## API Contracts

- **Endpoint (hergebruik):** `POST /api/staff`
- **Autorisatie:** bestaande `RequireManager` schrijfpolicy (beoogd voor Owner + Manager) blijft leidend.
- **Request (relevant):**
  - `firstName: string` (required, max 100)
  - `lastName: string` (required, max 100)
  - `email: string` (required, email, max 256)
  - `role: "manager" | "staff_member"`
  - `color: string`
  - `photoUrl?: string | null`
  - `schedule?: Record<string, ShiftBlock[]>`
- **Response:** bestaande `StaffMemberResponse` shape (inclusief `email`) blijft ongewijzigd.

## Out of Scope

- Aanpassen van staff edit-flow buiten noodzakelijke contractconsistentie.
- Wijzigen van Keycloak provisioninglogica of tenant-autharchitectuur.
- Bulk staff import, uitnodigingsflows of onboarding automation.
- RBAC-herontwerp of policywijzigingen buiten bestaand create-staff toegangsmodel.
