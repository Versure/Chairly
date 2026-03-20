# Manager krijgt 401 bij client toevoegen

## Overview

Wanneer een gebruiker met de rol **manager** een client probeert toe te voegen, ontvangt de applicatie een 401-respons. Dit hoort niet: managers moeten toegang hebben tot clients-functionaliteit (RequireStaff). Deze feature adresseert het structureel door backend-auth/tenantvalidatie te verharden, duidelijke foutdiagnostiek toe te voegen, regressietests in te bouwen en frontend-afhandeling voor 401/403 te verduidelijken.

## Domain Context

- Bounded context: Clients
- Key entities involved: Client, StaffMember (via auth context)
- Ubiquitous language: Client, Staff Member, Owner, Manager (see docs/domain-model.md)

## User Stories

- Als **manager** wil ik een client kunnen toevoegen zonder onterechte 401, zodat ik operationeel werk kan uitvoeren.
- Als **owner/manager/staff member** wil ik consistente autorisatie-uitkomsten per endpointgroep, zodat rechten voorspelbaar zijn.
- Als **developer** wil ik snelle diagnose van auth-fouten (issuer/audience/tenant/claims), zodat incidenten snel op te lossen zijn.

## Backend Tasks

### B1 — Versterk authenticatie- en tenantdiagnostiek voor 401-fouten

- Voeg gerichte tests toe voor claims- en tenantcontextverwerking:
  - geldige manager-claims + geldige tenantcontext -> geen 401.
  - ongeldige/missende `iss` -> 401.
  - ongeldige/missende `sub` -> 401.
  - non-GUID realm zonder geldige `Keycloak:TenantId` fallback -> 401.
- Voeg tests toe voor role claim transformation (`realm_access.roles` -> `ClaimTypes.Role`) inclusief randgevallen.
- Zorg dat logging/diagnostiek onderscheid maakt tussen oorzaakcategorieen.

### B2 — Voeg autorisatie-regressietests toe voor manager op clients endpoints

- Integratie-/endpointtests voor `/api/clients` groep:
  - owner -> toegestaan.
  - manager -> toegestaan.
  - staff_member -> toegestaan.
  - onbekende/lege rol -> 403.
  - authentisatie-/tenantfout -> 401.
- Verifieer dat policy-gedrag overeenkomt met `RequireStaff`.

## Frontend Tasks

### F1 — Verbeter frontend-afhandeling van 401 en 403 bij clients mutaties

- Controleer en harmoniseer afhandeling in clients data-access/store componenten.
- Toon onderscheidende foutboodschappen (Nederlands):
  - 401: sessie verlopen/ongeldig.
  - 403: onvoldoende rechten.
- Unit tests op store/service gedrag voor beide statuscodes.

### F2 — Voeg role-based guard en e2e regressietests toe

- Voeg ontbrekende unit tests toe rond role-guardgedrag voor `owner/manager/staff_member`.
- Playwright e2e regressies:
  - manager kan client toevoegen zonder auth-fout.
  - scenario met geforceerde 401 toont juiste melding/flow.
  - scenario met 403 toont juiste melding/flow.
- Assertions tegen Nederlandse UI-teksten.

## Acceptance Criteria

- [ ] Een manager-token kan succesvol `POST /api/clients` uitvoeren (geen 401 door onterechte auth/tenantfout).
- [ ] Auth-fouten door ongeldige issuer/audience/tenant-subject blijven 401 en bevatten bruikbare diagnostiek (zonder gevoelige data te lekken).
- [ ] Rolfouten blijven 403 (niet 401), zodat onderscheid tussen authenticatie en autorisatie helder is.
- [ ] Frontend toont consistente UX voor 401 (opnieuw inloggen/sessie verlopen) en 403 (geen toegang) bij clientmutaties.
- [ ] Regressietests dekken de rolmatrix voor clients-endpoints (owner/manager/staff_member + negatieve cases).
- [ ] All backend quality checks pass (dotnet build, test, format)
- [ ] All frontend quality checks pass (lint, test, build)
- [ ] Playwright e2e tests pass

## Domain Model Changes

- Geen nieuwe domein-entiteiten verwacht.
- Wel expliciete bevestiging van rol-ubiquitous language:
  - `owner`
  - `manager`
  - `staff_member`
- Tenant-resolutie blijft leidend voor `TenantId` afleiding uit tokencontext; feature richt zich op robuustheid en testdekking, niet op domeinmodelwijziging.

## API Contracts

- Bestaand contract blijft leidend:
  - `POST /api/clients`
  - Request: bestaand create-client payloadmodel
  - Response: bestaand create-client resultaatmodel
- Gedragsafspraken die expliciet gemaakt worden:
  - **201/200** voor geautoriseerde rollen volgens policy.
  - **401** alleen voor authentisatie-/tenantcontextproblemen (token validatie, issuer/audience, subject/tenant-resolutie).
  - **403** voor onvoldoende rolrechten.
- Diagnostiek:
  - server logging/events met oorzaakcategorie (issuer, audience, tenant mapping, claim parsing) zonder PII/secrets in response.

## Out of Scope

- Wijzigen van businessregels rond wie clients mag beheren (policy blijft `RequireStaff`).
- Introductie van nieuwe rollen of RBAC-modelwijzigingen.
- Grote herstructurering van Keycloak/infra buiten benodigde config-validatie en testbaarheid.
