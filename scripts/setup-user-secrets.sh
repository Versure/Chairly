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
