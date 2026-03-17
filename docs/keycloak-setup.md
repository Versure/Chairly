# Keycloak Local Development Setup

After starting the AppHost for the first time, Keycloak will be running with no realms configured.
Follow these steps to set up the default development realm.

## Prerequisites

- AppHost is running (`dotnet run --project src/backend/Chairly.AppHost`)
- Keycloak Admin UI is accessible via the Aspire dashboard link (typically `http://localhost:8080/admin`)
- Login with `admin` / value of `keycloak-admin-password` parameter (default: `admin`)

## 1. Create the default realm

1. Click **Create realm**
2. Set **Realm name** to `00000000-0000-0000-0000-000000000001`
3. Ensure **Enabled** is toggled on
4. Click **Create**

## 2. Create the frontend OIDC client (`chairly-frontend`)

1. Go to **Clients** > **Create client**
2. **Client ID**: `chairly-frontend`
3. **Client type**: OpenID Connect
4. Click **Next**
5. Enable **Standard flow**
6. Disable **Client authentication** (this is a public client)
7. Click **Next**
8. **Valid redirect URIs**: `http://localhost:4200/*`
9. **Web origins**: `http://localhost:4200`
10. Click **Save**

## 3. Create the admin service account client (`chairly-admin`)

1. Go to **Clients** > **Create client**
2. **Client ID**: `chairly-admin`
3. **Client type**: OpenID Connect
4. Click **Next**
5. Enable **Client authentication**
6. Enable **Service accounts roles**
7. Disable **Standard flow** and **Direct access grants**
8. Click **Next** then **Save**
9. Go to the **Credentials** tab, copy the **Client secret** and update the
   `keycloak-admin-client-secret` parameter in `appsettings.json` (or user secrets)
10. Go to **Service account roles** tab > **Assign role** > Filter by clients >
    Select `realm-management` roles: `manage-users`, `manage-realm`

## 4. Create realm roles

1. Go to **Realm roles** > **Create role**
2. Create three roles:
   - `owner`
   - `manager`
   - `staff_member`

## 5. Create a test user

1. Go to **Users** > **Add user**
2. **Username**: `owner@chairly.local`
3. **Email**: `owner@chairly.local`
4. **First name**: `Test`
5. **Last name**: `Owner`
6. **Email verified**: Yes
7. Click **Create**
8. Go to the **Credentials** tab > **Set password** > set to `owner` > disable **Temporary**
9. Go to the **Role mapping** tab > **Assign role** > select `owner`

## Notes

- The realm name matches `TenantConstants.DefaultTenantId` (`00000000-0000-0000-0000-000000000001`)
- The data volume `keycloak-data` persists configuration across AppHost restarts
- To reset Keycloak data: `docker volume rm $(docker volume ls -q --filter name=keycloak-data)`
