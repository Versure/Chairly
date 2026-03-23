# GitHub Variables and Secrets for CI/CD

This document lists the GitHub repository variables and secrets required for
workflows that run the Aspire AppHost (e.g. future deployment pipelines).

Current CI workflows (`backend-ci.yml`) only build and test — they do not run
Aspire, so these values are not needed for CI today. They will be required when
deployment pipelines are added.

## GitHub Variables

Configure under **Settings > Secrets and variables > Actions > Variables**.

| Variable | Value | Description |
|---|---|---|
| `RABBITMQ_USER` | `chairly` | RabbitMQ management username |
| `DEFAULT_REALM` | `chairly` | Default Keycloak realm name |
| `DEFAULT_TENANT_ID` | `00000000-0000-0000-0000-000000000001` | Default tenant identifier |
| `SMTP_FROM_ADDRESS` | `noreply@chairly.local` | SMTP sender email address |
| `SMTP_FROM_NAME` | `Chairly` | SMTP sender display name |
| `KEYCLOAK_CLIENT_ID` | `chairly-frontend` | Keycloak frontend client ID |
| `KEYCLOAK_ADMIN_CLIENT_ID` | `chairly-admin` | Keycloak admin service account client ID |
| `KEYCLOAK_ADMIN_PORTAL_REALM` | `chairly-admin` | Keycloak admin portal realm |
| `KEYCLOAK_ADMIN_PORTAL_CLIENT_ID` | `chairly-admin-portal` | Keycloak admin portal client ID |
| `KEYCLOAK_SMTP_HOST` | `maildev` | SMTP host as seen from Docker containers |
| `KEYCLOAK_SMTP_PORT` | `1025` | SMTP port as seen from Docker containers |

## GitHub Secrets

Configure under **Settings > Secrets and variables > Actions > Secrets**.

| Secret | Description |
|---|---|
| `RABBITMQ_PASSWORD` | RabbitMQ management password |
| `KEYCLOAK_ADMIN_PASSWORD` | Keycloak admin console password |
| `KEYCLOAK_ADMIN_CLIENT_SECRET` | Keycloak admin service account client secret |

## Mapping to AppHost Configuration

In a deployment workflow, map these to environment variables or Aspire parameters:

```yaml
env:
  # Aspire parameters (Parameters: prefix)
  Parameters__rabbitmq-user: ${{ vars.RABBITMQ_USER }}
  Parameters__rabbitmq-password: ${{ secrets.RABBITMQ_PASSWORD }}
  Parameters__keycloak-admin-password: ${{ secrets.KEYCLOAK_ADMIN_PASSWORD }}
  Parameters__keycloak-admin-client-secret: ${{ secrets.KEYCLOAK_ADMIN_CLIENT_SECRET }}

  # Application configuration
  Chairly__DefaultRealm: ${{ vars.DEFAULT_REALM }}
  Chairly__DefaultTenantId: ${{ vars.DEFAULT_TENANT_ID }}

  # SMTP
  Smtp__FromAddress: ${{ vars.SMTP_FROM_ADDRESS }}
  Smtp__FromName: ${{ vars.SMTP_FROM_NAME }}

  # Keycloak
  Keycloak__ClientId: ${{ vars.KEYCLOAK_CLIENT_ID }}
  Keycloak__AdminClientId: ${{ vars.KEYCLOAK_ADMIN_CLIENT_ID }}
  Keycloak__AdminPortalRealm: ${{ vars.KEYCLOAK_ADMIN_PORTAL_REALM }}
  Keycloak__AdminPortalClientId: ${{ vars.KEYCLOAK_ADMIN_PORTAL_CLIENT_ID }}
  Keycloak__SmtpHost: ${{ vars.KEYCLOAK_SMTP_HOST }}
  Keycloak__SmtpPort: ${{ vars.KEYCLOAK_SMTP_PORT }}
```
