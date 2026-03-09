import { expect, test } from './fixtures';

test('has title', async ({ page }) => {
  await page.goto('/');

  // App redirects to /diensten — expect h1 to contain 'Diensten'.
  await expect(page.locator('h1')).toContainText('Diensten');
});
