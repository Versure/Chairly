import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
    // Mock /api/config so the app can bootstrap without a real backend.
    // keycloakUrl points to a local path so Playwright intercepts it instantly.
    await page.route('**/api/config', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          keycloakUrl: 'http://localhost:4200/keycloak-mock',
          keycloakRealm: 'test',
          keycloakClientId: 'test',
        }),
      }),
    );

    // Abort Keycloak OIDC discovery requests so keycloak.init() fails instantly.
    await page.route('**/keycloak-mock/**', (route) => route.abort());

    await page.route('**/api/**', (route) => {
      const url = route.request().url();
      // eslint-disable-next-line no-console -- e2e test warning, not production code
      console.warn(`[e2e] Un-mocked API call intercepted: ${route.request().method()} ${url}`);
      return route.fulfill({
        status: 599,
        contentType: 'application/json',
        body: JSON.stringify({ error: `Un-mocked API route: ${url}` }),
      });
    });
    await use(page);
  },
});

export { expect } from '@playwright/test';
