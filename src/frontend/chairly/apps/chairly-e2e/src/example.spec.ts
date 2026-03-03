import { expect, test } from '@playwright/test';

test('has title', async ({ page }) => {
  await page.goto('/');

  // App redirects to /services — expect h1 to contain 'Services'.
  await expect(page.locator('h1')).toContainText('Services');
});
