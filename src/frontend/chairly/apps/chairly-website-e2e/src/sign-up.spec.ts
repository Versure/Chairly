import { expect, test } from '@playwright/test';

test.describe('Sign Up Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock the API endpoint
    await page.route('**/api/onboarding/sign-up-requests', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '00000000-0000-0000-0000-000000000002',
            salonName: 'Test Salon',
            ownerFirstName: 'Jan',
            ownerLastName: 'Jansen',
            email: 'jan@salon.nl',
            createdAtUtc: '2026-01-01T00:00:00Z',
          }),
        });
      }
      return route.continue();
    });

    await page.goto('/aanmelden');
  });

  test('should display page heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Aanmelden' })).toBeVisible();
  });

  test('should submit form and navigate to confirmation page', async ({ page }) => {
    await page.getByLabel('Salonnaam').fill('Test Salon');
    await page.getByLabel('Voornaam').fill('Jan');
    await page.getByLabel('Achternaam').fill('Jansen');
    await page.getByLabel('E-mailadres').fill('jan@salon.nl');
    await page.getByRole('button', { name: 'Aanmelden' }).click();

    await expect(page).toHaveURL(/\/bevestiging\?type=aanmelding/);
    await expect(page.getByText('Bedankt voor uw aanmelding!')).toBeVisible();
  });

  test('should show validation errors for empty required fields', async ({ page }) => {
    await page.getByRole('button', { name: 'Aanmelden' }).click();

    await expect(page.getByText('Salonnaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('Voornaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('Achternaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('E-mailadres is verplicht.', { exact: true })).toBeVisible();
  });
});
