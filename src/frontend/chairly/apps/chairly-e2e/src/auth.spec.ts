import { expect, test } from './fixtures';

/**
 * Authentication E2E tests.
 *
 * These tests verify that the mock Keycloak OIDC flow works correctly
 * and that the authenticated app renders properly.
 */

test('app bootstraps with mock authentication and shows main content', async ({ page }) => {
  // Mock the services API so the default redirect to /diensten works
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));

  await page.goto('/diensten');

  await expect(page.getByRole('heading', { name: 'Diensten', level: 1 })).toBeVisible();
});

test('authenticated user name is visible in the shell', async ({ page }) => {
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));

  await page.goto('/diensten');

  // The AuthStore loads the profile from the Keycloak mock account endpoint.
  // The shell should display the user's name.
  await expect(page.getByText('Test User')).toBeVisible();
});
