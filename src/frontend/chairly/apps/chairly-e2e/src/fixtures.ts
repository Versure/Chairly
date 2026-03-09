import { test as base } from '@playwright/test';

export const test = base.extend({
  page: async ({ page }, use) => {
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
