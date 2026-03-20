# Review Findings — staff-create-email-alignment

## Auto-fixed
- Geen

## Needs human judgment
- Geen

## All clear
- Backend contract voor create-staff bevat verplicht `Email` met `[Required]`, `[EmailAddress]`, `[MaxLength(256)]` en gebruikt bestaand endpoint `POST /api/staff` met `RequireManager` policy.
- Backend regressietests toegevoegd voor ontbrekende/ongeldige e-mail en autorisatie-matrix (owner/manager toegestaan, andere rollen geweigerd).
- Frontend `CreateStaffMemberRequest` bevat `email` en verstuurt dit naar `POST /api/staff`.
- Staff-form-dialog bevat verplicht e-mailveld met Nederlandse label/foutteksten, inline required/format/maxlength validatie en submit-gating.
- API-validatiefouten op e-mail worden in create-flow gemapt naar Nederlandse formulier- en veldfout.
- Staff update/deactivate/reactivate paden blijven functioneel (geen regressie in aangepaste tests).
- Quality checks geslaagd: backend build/test/format; frontend lint/test/build; gerichte Playwright staff e2e-tests geslaagd.
