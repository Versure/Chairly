import { expect, test } from './fixtures';

/**
 * Create a mock JWT (unsigned) with the given payload claims.
 */
function createMockJwt(claims: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify(claims)).toString('base64url');
  return `${header}.${payload}.`;
}

function tokenClaimsForRole(role: string): Record<string, unknown> {
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

async function overrideTokenWithRole(
  page: import('@playwright/test').Page,
  role: string,
): Promise<void> {
  const claims = tokenClaimsForRole(role);
  await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/token', (route) => {
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        access_token: createMockJwt(claims),
        refresh_token: createMockJwt({ ...claims, typ: 'Refresh' }),
        id_token: createMockJwt({ ...claims, typ: 'ID', nonce: '' }),
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
  await overrideTokenWithRole(page, 'owner');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
  await expect(page.getByText('Toegang geweigerd')).toBeHidden();
});

test('manager can access settings page', async ({ page }) => {
  await overrideTokenWithRole(page, 'manager');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
  await expect(page.getByText('Toegang geweigerd')).toBeHidden();
});

test('staff member gets 403 on settings page', async ({ page }) => {
  await overrideTokenWithRole(page, 'staff_member');
  await setupSettingsApiMocks(page);

  await page.goto('/instellingen');

  await expect(page.getByText('Toegang geweigerd')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeHidden();
});
