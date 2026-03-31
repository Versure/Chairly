# AppHost Credentials Externalization

> **Status: Implemented** — Merged to main.

## Overview

The Chairly repository is **public**. Credentials and environment-specific configuration values are currently hardcoded in `src/backend/Chairly.AppHost/appsettings.json` and `Program.cs`. These must be removed from version control entirely. For local development, values are provided via `dotnet user-secrets` (the `UserSecretsId` already exists in the `.csproj`). For CI, non-secret values use GitHub variables and secrets use GitHub repository secrets. An `.env.example` file documents all required variables for developer onboarding.

## Domain Context

- Bounded context: **Infrastructure / cross-cutting** (no domain entities affected)
- Key components involved: `Chairly.AppHost`, GitHub Actions workflows
- No domain model changes, no API changes, no frontend changes

## Values to Externalize

The following table lists every value that must be removed from committed files and sourced from user-secrets (local) or GitHub variables/secrets (CI).

| Key | Current location | Current value | Secret? | User-secrets key | GitHub mechanism |
|-----|-----------------|---------------|---------|-----------------|-----------------|
| `Parameters:rabbitmq-user` | `AppHost/appsettings.json` | `chairly` | No | `Parameters:rabbitmq-user` | Variable `RABBITMQ_USER` |
| `Parameters:rabbitmq-password` | `AppHost/appsettings.json` | `chairly` | Yes | `Parameters:rabbitmq-password` | Secret `RABBITMQ_PASSWORD` |
| `Parameters:keycloak-admin-password` | `AppHost/appsettings.json` | `admin` | Yes | `Parameters:keycloak-admin-password` | Secret `KEYCLOAK_ADMIN_PASSWORD` |
| `Parameters:keycloak-admin-client-secret` | `AppHost/appsettings.json` | `chairly-admin-secret` | Yes | `Parameters:keycloak-admin-client-secret` | Secret `KEYCLOAK_ADMIN_CLIENT_SECRET` |
| `defaultRealm` | `AppHost/Program.cs` (hardcoded) | `chairly` | No | `Chairly:DefaultRealm` | Variable `DEFAULT_REALM` |
| `defaultTenantId` | `AppHost/Program.cs` (hardcoded) | `00000000-...0001` | No | `Chairly:DefaultTenantId` | Variable `DEFAULT_TENANT_ID` |
| SMTP from address | `AppHost/Program.cs` (hardcoded) | `noreply@chairly.local` | No | `Smtp:FromAddress` | Variable `SMTP_FROM_ADDRESS` |
| SMTP from name | `AppHost/Program.cs` (hardcoded) | `Chairly` | No | `Smtp:FromName` | Variable `SMTP_FROM_NAME` |
| Keycloak client ID | `AppHost/Program.cs` (hardcoded) | `chairly-frontend` | No | `Keycloak:ClientId` | Variable `KEYCLOAK_CLIENT_ID` |
| Keycloak admin client ID | `AppHost/Program.cs` (hardcoded) | `chairly-admin` | No | `Keycloak:AdminClientId` | Variable `KEYCLOAK_ADMIN_CLIENT_ID` |
| Keycloak admin portal realm | `AppHost/Program.cs` (hardcoded) | `chairly-admin` | No | `Keycloak:AdminPortalRealm` | Variable `KEYCLOAK_ADMIN_PORTAL_REALM` |
| Keycloak admin portal client ID | `AppHost/Program.cs` (hardcoded) | `chairly-admin-portal` | No | `Keycloak:AdminPortalClientId` | Variable `KEYCLOAK_ADMIN_PORTAL_CLIENT_ID` |
| Keycloak SMTP host (Docker) | `AppHost/Program.cs` (hardcoded) | `maildev` | No | `Keycloak:SmtpHost` | Variable `KEYCLOAK_SMTP_HOST` |
| Keycloak SMTP port (Docker) | `AppHost/Program.cs` (hardcoded) | `1025` | No | `Keycloak:SmtpPort` | Variable `KEYCLOAK_SMTP_PORT` |

Additionally, the **API project** has hardcoded values that should also be reviewed:

| Key | Current location | Current value | Secret? |
|-----|-----------------|---------------|---------|
| `ConnectionStrings:ChairlyDb` | `Api/appsettings.Development.json` | `Host=localhost;...Password=postgres` | Yes |
| `ConnectionStrings:WebsiteDb` | `Api/appsettings.Development.json` | `Host=localhost;...Password=postgres` | Yes |
| `Onboarding:AdminEmail` | `Api/appsettings.json` | `admin@chairly.nl` | No |

Note: The API connection strings in `appsettings.Development.json` contain `Password=postgres`. When running via Aspire, these are overridden by Aspire's service discovery. However, since the repo is public, they should still be removed. The `Onboarding:AdminEmail` is a business configuration value, not a credential -- it can stay in `appsettings.json` unless you want to externalize it too. The connection strings should be moved to user-secrets for the API project as well.

---

## Infrastructure Tasks

### I1 — Remove hardcoded credentials from AppHost appsettings.json

Remove the `Parameters` section from `src/backend/Chairly.AppHost/appsettings.json` entirely. The file should only retain the `Logging` section.

**File to modify:** `src/backend/Chairly.AppHost/appsettings.json`

**Before:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  },
  "Parameters": {
    "rabbitmq-user": "chairly",
    "rabbitmq-password": "chairly",
    "keycloak-admin-password": "admin",
    "keycloak-admin-client-secret": "chairly-admin-secret"
  }
}
```

**After:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  }
}
```

**Tests:**
- Verify `appsettings.json` does not contain the string `Parameters`
- Verify `appsettings.json` does not contain any of the old credential values

---

### I2 — Externalize hardcoded values in AppHost Program.cs

Replace all hardcoded environment-specific values in `src/backend/Chairly.AppHost/Program.cs` with configuration reads. The values come from user-secrets locally (which feed into the standard `IConfiguration` via `builder.Configuration`).

**File to modify:** `src/backend/Chairly.AppHost/Program.cs`

**Changes:**

1. Replace the hardcoded `defaultRealm` and `defaultTenantId` variables:
```csharp
// Before
var defaultRealm = "chairly";
var defaultTenantId = "00000000-0000-0000-0000-000000000001";

// After
var defaultRealm = builder.Configuration["Chairly:DefaultRealm"]
    ?? throw new InvalidOperationException("Chairly:DefaultRealm is not configured. Run: dotnet user-secrets set \"Chairly:DefaultRealm\" \"chairly\"");
var defaultTenantId = builder.Configuration["Chairly:DefaultTenantId"]
    ?? throw new InvalidOperationException("Chairly:DefaultTenantId is not configured. Run: dotnet user-secrets set \"Chairly:DefaultTenantId\" \"00000000-0000-0000-0000-000000000001\"");
```

2. Replace hardcoded SMTP values in the `.WithEnvironment(...)` calls:
```csharp
// Before
.WithEnvironment("Smtp__FromAddress", "noreply@chairly.local")
.WithEnvironment("Smtp__FromName", "Chairly")

// After
.WithEnvironment("Smtp__FromAddress", builder.Configuration["Smtp:FromAddress"]
    ?? throw new InvalidOperationException("Smtp:FromAddress is not configured."))
.WithEnvironment("Smtp__FromName", builder.Configuration["Smtp:FromName"]
    ?? throw new InvalidOperationException("Smtp:FromName is not configured."))
```

3. Replace hardcoded Keycloak client IDs and related config:
```csharp
// Before
.WithEnvironment("Keycloak__ClientId", "chairly-frontend")
.WithEnvironment("Keycloak__AdminClientId", "chairly-admin")
.WithEnvironment("Keycloak__AdminPortalRealm", "chairly-admin")
.WithEnvironment("Keycloak__AdminPortalClientId", "chairly-admin-portal")
.WithEnvironment("Keycloak__SmtpHost", "maildev")
.WithEnvironment("Keycloak__SmtpPort", "1025")

// After -- read from configuration
.WithEnvironment("Keycloak__ClientId", builder.Configuration["Keycloak:ClientId"]
    ?? throw new InvalidOperationException("Keycloak:ClientId is not configured."))
.WithEnvironment("Keycloak__AdminClientId", builder.Configuration["Keycloak:AdminClientId"]
    ?? throw new InvalidOperationException("Keycloak:AdminClientId is not configured."))
.WithEnvironment("Keycloak__AdminPortalRealm", builder.Configuration["Keycloak:AdminPortalRealm"]
    ?? throw new InvalidOperationException("Keycloak:AdminPortalRealm is not configured."))
.WithEnvironment("Keycloak__AdminPortalClientId", builder.Configuration["Keycloak:AdminPortalClientId"]
    ?? throw new InvalidOperationException("Keycloak:AdminPortalClientId is not configured."))
.WithEnvironment("Keycloak__SmtpHost", builder.Configuration["Keycloak:SmtpHost"]
    ?? throw new InvalidOperationException("Keycloak:SmtpHost is not configured."))
.WithEnvironment("Keycloak__SmtpPort", builder.Configuration["Keycloak:SmtpPort"]
    ?? throw new InvalidOperationException("Keycloak:SmtpPort is not configured."))
```

4. The existing `builder.AddParameter(...)` calls for `rabbitmq-user`, `rabbitmq-password`, `keycloak-admin-password`, and `keycloak-admin-client-secret` already read from `IConfiguration` (under the `Parameters:` prefix) -- no code change needed for those. They will now resolve from user-secrets instead of `appsettings.json`.

**Tests:**
- Verify `Program.cs` no longer contains any of the hardcoded string literals: `"chairly-frontend"`, `"chairly-admin"`, `"chairly-admin-portal"`, `"noreply@chairly.local"`, `"Chairly"` (as SMTP name), `"maildev"`, `"1025"` (as SMTP port), `"chairly"` (as realm), `"00000000-0000-0000-0000-000000000001"`
- Verify the app fails fast with a descriptive error message when a required config value is missing

---

### I3 — Remove hardcoded connection strings from API appsettings.Development.json

The API project's `appsettings.Development.json` contains connection strings with `Password=postgres`. When running via Aspire, these are overridden, but since the repo is public they should be removed.

**File to modify:** `src/backend/Chairly.Api/appsettings.Development.json`

**Before:**
```json
{
  "Logging": { ... },
  "ConnectionStrings": {
    "ChairlyDb": "Host=localhost;Database=chairly_dev;Username=postgres;Password=postgres",
    "WebsiteDb": "Host=localhost;Database=website_dev;Username=postgres;Password=postgres"
  },
  "Keycloak": { ... },
  "Onboarding": { ... }
}
```

**After:** Remove the `ConnectionStrings` section entirely.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Keycloak": {
    "AdminPortalRealm": "chairly-admin",
    "AdminPortalClientId": "chairly-admin-portal"
  },
  "Onboarding": {
    "AdminEmail": "admin@chairly.nl"
  }
}
```

Note: When running via Aspire, connection strings are injected automatically. For standalone API development without Aspire, developers would use `dotnet user-secrets` on the API project to set connection strings.

**Tests:**
- Verify no `appsettings*.json` file in the repository contains `Password=`

---

### I4 — Seed user-secrets with default development values

Provide a script or documented commands to set all required user-secrets for local development. The AppHost project already has `UserSecretsId: 5eb18f1e-519a-4392-8d3c-18b08781fdeb` in its `.csproj`.

**Create file:** `scripts/setup-user-secrets.sh`

This script runs `dotnet user-secrets set` for each required value using the AppHost project path. It should be idempotent (safe to run multiple times).

```bash
#!/usr/bin/env bash
set -euo pipefail

PROJECT="src/backend/Chairly.AppHost/Chairly.AppHost.csproj"

echo "Setting user-secrets for Chairly.AppHost..."

# Aspire parameters (used by builder.AddParameter)
dotnet user-secrets set "Parameters:rabbitmq-user" "chairly" --project "$PROJECT"
dotnet user-secrets set "Parameters:rabbitmq-password" "chairly" --project "$PROJECT"
dotnet user-secrets set "Parameters:keycloak-admin-password" "admin" --project "$PROJECT"
dotnet user-secrets set "Parameters:keycloak-admin-client-secret" "chairly-admin-secret" --project "$PROJECT"

# Application configuration (used by builder.Configuration[...])
dotnet user-secrets set "Chairly:DefaultRealm" "chairly" --project "$PROJECT"
dotnet user-secrets set "Chairly:DefaultTenantId" "00000000-0000-0000-0000-000000000001" --project "$PROJECT"

# SMTP
dotnet user-secrets set "Smtp:FromAddress" "noreply@chairly.local" --project "$PROJECT"
dotnet user-secrets set "Smtp:FromName" "Chairly" --project "$PROJECT"

# Keycloak
dotnet user-secrets set "Keycloak:ClientId" "chairly-frontend" --project "$PROJECT"
dotnet user-secrets set "Keycloak:AdminClientId" "chairly-admin" --project "$PROJECT"
dotnet user-secrets set "Keycloak:AdminPortalRealm" "chairly-admin" --project "$PROJECT"
dotnet user-secrets set "Keycloak:AdminPortalClientId" "chairly-admin-portal" --project "$PROJECT"
dotnet user-secrets set "Keycloak:SmtpHost" "maildev" --project "$PROJECT"
dotnet user-secrets set "Keycloak:SmtpPort" "1025" --project "$PROJECT"

echo "Done. All user-secrets configured for local development."
```

**Also create file:** `.env.example` (at repo root, for documentation only -- not loaded by the app)

This file documents every required environment variable with placeholder values, so developers know what to configure:

```
# Chairly AppHost -- required user-secrets
# Run: bash scripts/setup-user-secrets.sh
# Or set individually with: dotnet user-secrets set "<key>" "<value>" --project src/backend/Chairly.AppHost/Chairly.AppHost.csproj

# Aspire parameters
Parameters__rabbitmq-user=chairly
Parameters__rabbitmq-password=chairly
Parameters__keycloak-admin-password=admin
Parameters__keycloak-admin-client-secret=<your-secret>

# Application config
Chairly__DefaultRealm=chairly
Chairly__DefaultTenantId=00000000-0000-0000-0000-000000000001

# SMTP
Smtp__FromAddress=noreply@chairly.local
Smtp__FromName=Chairly

# Keycloak
Keycloak__ClientId=chairly-frontend
Keycloak__AdminClientId=chairly-admin
Keycloak__AdminPortalRealm=chairly-admin
Keycloak__AdminPortalClientId=chairly-admin-portal
Keycloak__SmtpHost=maildev
Keycloak__SmtpPort=1025
```

**Tests:**
- Verify `scripts/setup-user-secrets.sh` runs without errors
- Verify the AppHost starts successfully after running the script (manual verification)

---

### I5 — Document GitHub variables and secrets for CI/CD

Document the GitHub repository variables and secrets that must be configured for any workflow that runs the AppHost (e.g. future deployment pipelines). The current CI workflows only build/test and do not run Aspire, so no workflow file changes are needed now — the `builder.AddParameter(...)` and `builder.Configuration[...]` calls are runtime-only, not compile-time.

If the build does fail after I1/I2 (unlikely), add the required variables/secrets to `.github/workflows/backend-ci.yml` as environment variables.

**Deliverable:** Add a `docs/github-secrets.md` file documenting the required GitHub configuration:

**GitHub Variables** (Settings > Secrets and variables > Actions > Variables):
- `RABBITMQ_USER` = `chairly`
- `DEFAULT_REALM` = `chairly`
- `DEFAULT_TENANT_ID` = `00000000-0000-0000-0000-000000000001`
- `SMTP_FROM_ADDRESS` = `noreply@chairly.local`
- `SMTP_FROM_NAME` = `Chairly`
- `KEYCLOAK_CLIENT_ID` = `chairly-frontend`
- `KEYCLOAK_ADMIN_CLIENT_ID` = `chairly-admin`
- `KEYCLOAK_ADMIN_PORTAL_REALM` = `chairly-admin`
- `KEYCLOAK_ADMIN_PORTAL_CLIENT_ID` = `chairly-admin-portal`
- `KEYCLOAK_SMTP_HOST` = `maildev`
- `KEYCLOAK_SMTP_PORT` = `1025`

**GitHub Secrets** (Settings > Secrets and variables > Actions > Secrets):
- `RABBITMQ_PASSWORD`
- `KEYCLOAK_ADMIN_PASSWORD`
- `KEYCLOAK_ADMIN_CLIENT_SECRET`

**Tests:**
- Verify `dotnet build src/backend/Chairly.slnx` passes after credential removal
- Verify `dotnet test src/backend/Chairly.slnx` passes after credential removal
- Verify CI pipeline passes (check after push)

---

### I6 — Verify no secrets remain and add production warning

Since the repository is public and credentials have been committed to git history, verify the cleanup is complete and add safety warnings.

**Action items:**
1. Scan all committed files for remaining hardcoded passwords, secrets, or connection strings containing credentials
2. Add a warning header to `.env.example` stating that the default values in `setup-user-secrets.sh` are for local development only and must not be reused in production
3. Verify `.gitignore` excludes `.env` and `.env.*` (confirmed: it does, with `!.env.example` exception)

Note: A full git history rewrite (e.g. `git filter-repo`) is out of scope for development-only defaults like `chairly`/`admin`.

**Tests:**
- Verify `.env.example` contains a "local development only — do not reuse in production" warning
- Verify `grep -r "Password=" src/backend/ --include="*.json"` returns no results in committed files
- Verify no `appsettings*.json` file contains credential values (`chairly-admin-secret`, `admin`, `postgres`)

---

## Acceptance Criteria

- [ ] `src/backend/Chairly.AppHost/appsettings.json` contains no credentials or parameters
- [ ] `src/backend/Chairly.AppHost/Program.cs` contains no hardcoded environment-specific strings
- [ ] `src/backend/Chairly.Api/appsettings.Development.json` contains no connection strings with passwords
- [ ] `scripts/setup-user-secrets.sh` exists and configures all required values
- [ ] `.env.example` exists at repo root documenting all required variables
- [ ] AppHost starts successfully after running `scripts/setup-user-secrets.sh`
- [ ] `dotnet build src/backend/Chairly.slnx` passes
- [ ] `dotnet test src/backend/Chairly.slnx` passes
- [ ] `dotnet format src/backend/Chairly.slnx --verify-no-changes` passes
- [ ] No file in the repository contains hardcoded passwords or secret tokens
- [ ] GitHub variables and secrets are documented for future CI/CD workflows

## Out of Scope

- Production deployment configuration (Kubernetes secrets, Azure Key Vault, etc.)
- Full git history rewrite to remove old commits containing credentials
- Frontend environment files (Angular `environment.ts` -- already handled separately)
- Keycloak realm configuration export/import automation
- `.env` file loading at runtime (using `dotnet user-secrets` instead)
