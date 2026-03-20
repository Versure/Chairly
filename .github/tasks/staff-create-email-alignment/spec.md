---
feature: staff-create-email-alignment
status: draft
branches:
  feature: feat/staff-create-email-alignment
  backend: impl/staff-create-email-alignment-backend
  frontend: impl/staff-create-email-alignment-frontend
tasks:
  - id: B1
    title: Bevestig en documenteer create-staff validatiecontract met verplicht e-mailadres
    layer: backend
    status: pending
    depends_on: []
  - id: B2
    title: Voeg backend regressietests toe voor e-mailvalidatie en autorisatieroute
    layer: backend
    status: pending
    depends_on: [B1]
  - id: F1
    title: Voeg verplicht e-mailadres toe aan medewerker-toevoegen dialoog en requestmodel
    layer: frontend
    status: pending
    depends_on: [B1]
  - id: F2
    title: Implementeer inline validatie plus API-foutmapping voor create-staff flow
    layer: frontend
    status: pending
    depends_on: [F1, B2]
  - id: R1
    title: Voer end-to-end regressiecontrole uit voor owner en manager staff-creatie
    layer: review
    status: pending
    depends_on: [B2, F2]
---

# Staff create email alignment

## Summary
Issue #73 (https://github.com/Versure/Chairly/issues/73) meldt dat bij **Medewerker toevoegen** een fout ontstaat omdat `Email` verplicht is in de backend, terwijl het veld ontbreekt in de frontend dialoog. Deze feature herstelt contract-alignement in de **Staff** bounded context door het bestaande create-staff endpoint te hergebruiken, e-mail expliciet verplicht te maken in de UI, en validatiegedrag consistent te maken tussen inline formulierfeedback en API-foutmapping. Autorisatie blijft op het bestaande manager-schrijfniveau (Owner + Manager via policygedrag).

## User Stories
- Als **Owner** wil ik een medewerker kunnen toevoegen met een verplicht e-mailadres, zodat provisioning naar identity correct verloopt.
- Als **Manager** wil ik dezelfde create-staff flow kunnen gebruiken als Owner, zodat operationeel personeelsbeheer mogelijk blijft.
- Als **gebruiker** wil ik directe inline validatiefouten én duidelijke serverfouten zien, zodat ik invoerproblemen snel kan corrigeren.
- Als **developer** wil ik dat frontend requestmodellen en backend commandvalidatie hetzelfde contract hanteren, zodat regressies op verplichte velden worden voorkomen.

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

## Domain Model Changes
- **Geen nieuwe entiteiten of value objects**.
- Bestaande **StaffMember** blijft leidend met bestaand attribuut `Email`.
- Geen wijziging in rolmodel of ubiquitous language:
  - `Owner`
  - `Manager`
  - `Staff Member`
- Geen statuskolommen toegevoegd; bestaande timestamp-conventies blijven intact conform ADR-009.

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
- **Foutafspraken:**
  - Validatiefouten op ontbrekende/ongeldige e-mail worden via bestaand validatiepad teruggegeven.
  - Keycloak-provisioningfouten blijven via bestaand `KeycloakError`-pad afgehandeld.

## UI/UX Description
- In `staff-form-dialog` wordt een nieuw invoerveld toegevoegd:
  - Label: **E-mailadres**
  - Type: email
  - Placeholder: `naam@salon.nl` (of vergelijkbaar)
- Inline validatie (Nederlands):
  - verplicht: “E-mailadres is verplicht.”
  - formaat: “Voer een geldig e-mailadres in.”
  - max lengte: passende melding volgens bestaand patroon.
- Submit blijft disabled bij ongeldig formulier.
- API-foutmapping:
  - servervalidatie op `email` koppelen aan formulierfout en/of algemene foutmelding.
  - foutteksten blijven Nederlands en consistent met bestaande staff UX.
- Geen nieuwe pagina of route; wijziging blijft binnen bestaande Staff featureflow.

## Test Requirements
- **Backend (B2):**
  - Unit tests voor `CreateStaffMemberCommand` validatie: ontbrekende e-mail, ongeldig e-mailformaat.
  - Handler/endpoint regressietests: geldige e-mail happy flow, policygedrag (Owner/Manager toegestaan, niet-geautoriseerd geblokkeerd).
  - Waarborg dat bestaand Keycloak-foutgedrag ongewijzigd blijft.
- **Frontend (F1/F2):**
  - Component tests voor `staff-form-dialog`: e-mailveld aanwezig, required + format validators, submit gating.
  - Service/store tests: `email` wordt meegestuurd in create request en API-validatiefouten worden correct gemapt.
  - E2E/flowtests voor staff aanmaken met Owner en Manager.

## Out of Scope
- Aanpassen van staff edit-flow buiten noodzakelijke contractconsistentie.
- Wijzigen van Keycloak provisioninglogica of tenant-autharchitectuur.
- Bulk staff import, uitnodigingsflows of onboarding automation.
- RBAC-herontwerp of policywijzigingen buiten bestaand create-staff toegangsmodel.
