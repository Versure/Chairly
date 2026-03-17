import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
    // Playwright routes match in LIFO order (last registered wins).
    // Register the catch-all FIRST so that specific routes registered later
    // (like /api/config) take priority over it.
    await page.route('**/api/**', (route) => {
      const url = route.request().url();
      console.warn(`[e2e] Un-mocked API call intercepted: ${route.request().method()} ${url}`);
      return route.fulfill({
        status: 599,
        contentType: 'application/json',
        body: JSON.stringify({ error: `Un-mocked API route: ${url}` }),
      });
    });

    // Mock /api/config so the app can bootstrap without a real backend.
    // keycloakUrl points to a local path so Playwright intercepts it instantly.
    // Registered AFTER the catch-all so it takes priority (LIFO).
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

    await use(page);
  },
});

export { expect } from '@playwright/test';
