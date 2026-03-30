import { expect, test } from './fixtures';

/**
 * Create a mock JWT (unsigned) with the given payload claims.
 */
function createMockJwt(claims: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify(claims)).toString('base64url');
  return `${header}.${payload}.`;
}

function baseClaimsForRole(role: string): Record<string, unknown> {
  return {
    exp: 9999999999,
    iat: Math.floor(Date.now() / 1000),
    sub: 'e2e-user',
    sid: 'mock-session-id',
    realm_access: { roles: [role] },
    resource_access: {},
    given_name: 'Test',
    family_name: 'User',
  };
}

/**
 * Override the Keycloak OIDC mock to issue tokens with a specific role.
 *
 * This replaces BOTH the auth redirect and the token endpoint so that
 * the nonce captured during the auth redirect is properly included in
 * the ID token (keycloak-js validates nonce match).
 */
async function setupAuthWithRole(
  page: import('@playwright/test').Page,
  role: string,
): Promise<void> {
  let capturedNonce = '';
  const claims = baseClaimsForRole(role);

  // Override auth redirect — capture nonce and redirect back with auth code.
  // Registered AFTER the fixture's route, so LIFO gives this priority.
  await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/auth**', (route) => {
    const requestUrl = new URL(route.request().url());
    const state = requestUrl.searchParams.get('state') ?? 'mock-state';
    capturedNonce = requestUrl.searchParams.get('nonce') ?? '';
    const redirectUri = requestUrl.searchParams.get('redirect_uri') ?? 'http://localhost:4200/';
    const responseMode = requestUrl.searchParams.get('response_mode') ?? 'fragment';

    let callbackUrl: string;
    if (responseMode === 'query') {
      const url = new URL(redirectUri);
      url.searchParams.set('state', state);
      url.searchParams.set('session_state', 'mock-session-id');
      url.searchParams.set('code', 'mock-auth-code');
      callbackUrl = url.toString();
    } else {
      const params = new URLSearchParams({
        state,
        session_state: 'mock-session-id',
        code: 'mock-auth-code',
      });
      callbackUrl = `${redirectUri}#${params.toString()}`;
    }

    return route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: `<!DOCTYPE html><html><head><script>window.location.replace(${JSON.stringify(callbackUrl)});</script></head><body></body></html>`,
    });
  });

  // Override token endpoint — return tokens with the captured nonce and specific role.
  await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/token', (route) => {
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        access_token: createMockJwt(claims),
        refresh_token: createMockJwt({ ...claims, typ: 'Refresh' }),
        id_token: createMockJwt({ ...claims, typ: 'ID', nonce: capturedNonce }),
        token_type: 'Bearer',
        expires_in: 86400,
        refresh_expires_in: 86400,
        session_state: 'mock-session-id',
      }),
    });
  });
}

async function setupSettingsApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/settings/company', (route) =>
    route.fulfill({
      json: {
        companyName: 'Test Salon',
        companyEmail: 'test@example.com',
        street: null,
        houseNumber: null,
        postalCode: null,
        city: null,
        companyPhone: null,
        ibanNumber: null,
        vatNumber: null,
        paymentPeriodDays: null,
      },
    }),
  );

  await page.route('**/api/settings/vat', (route) =>
    route.fulfill({
      json: { defaultVatRate: 21 },
    }),
  );
}

// --- Settings page access control tests ---

test('owner can access settings page', async ({ page }) => {
  await setupAuthWithRole(page, 'owner');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
  await expect(page.getByText('Toegang geweigerd')).toBeHidden();
});

test('manager can access settings page', async ({ page }) => {
  await setupAuthWithRole(page, 'manager');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
  await expect(page.getByText('Toegang geweigerd')).toBeHidden();
});

test('staff member gets 403 on settings page', async ({ page }) => {
  await setupAuthWithRole(page, 'staff_member');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByText('Toegang geweigerd')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeHidden();
});
