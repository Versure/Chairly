import { expect, test } from './fixtures';

/**
 * Authentication & Authorization E2E tests.
 *
 * These tests require a running Keycloak instance with a configured realm,
 * users, and roles. They are skipped by default and should be enabled in
 * environments where Keycloak is available (e.g. CI with docker-compose).
 */

test.describe('Authentication flow', () => {
  test.skip(true, 'Requires a running Keycloak instance');

  test('app redirects to Keycloak login on first visit', async ({ page }) => {
    await page.goto('/');

    // The app should redirect to the Keycloak login page
    await expect(page).toHaveURL(/\/realms\/.*\/protocol\/openid-connect\/auth/);
  });

  test('clicking "Uitloggen" redirects to Keycloak logout', async ({ page }) => {
    // Assumes user is already logged in via Keycloak
    await page.goto('/diensten');

    const logoutButton = page.getByRole('button', { name: 'Uitloggen' });
    await expect(logoutButton).toBeVisible();
    await logoutButton.click();

    // After logout, the app should redirect to the Keycloak login page
    await expect(page).toHaveURL(/\/realms\/.*\/protocol\/openid-connect/);
  });
});

test.describe('Role-based visibility', () => {
  test.skip(true, 'Requires a running Keycloak instance');

  test('staff_member does not see "Facturatie" nav item', async ({ page }) => {
    // Log in as a staff_member user (Keycloak test user with staff_member role)
    await page.goto('/diensten');

    // The "Facturen" nav link should not be visible for staff_member
    const facturenLink = page.getByRole('link', { name: 'Facturen' });
    await expect(facturenLink).toBeHidden();
  });
});

test.describe('Unauthorized route access', () => {
  test.skip(true, 'Requires a running Keycloak instance');

  test('staff_member navigating to manager-only route is redirected to /toegang-geweigerd', async ({
    page,
  }) => {
    // Log in as a staff_member user, then manually navigate to a manager-only route
    await page.goto('/facturen');

    // The role guard should redirect to the access denied page
    await expect(page).toHaveURL(/\/toegang-geweigerd/);
  });
});
