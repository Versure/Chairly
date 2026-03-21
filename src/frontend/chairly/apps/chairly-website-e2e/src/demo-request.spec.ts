import { expect, test } from '@playwright/test';

test.describe('Demo Request Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock the API endpoint
    await page.route('**/api/onboarding/demo-requests', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '00000000-0000-0000-0000-000000000001',
            contactName: 'Test Gebruiker',
            salonName: 'Test Salon',
            email: 'test@salon.nl',
            createdAtUtc: '2026-01-01T00:00:00Z',
          }),
        });
      }
      return route.continue();
    });

    await page.goto('/demo-aanvragen');
  });

  test('should display page heading', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Demo aanvragen' })).toBeVisible();
  });

  test('should submit form and navigate to confirmation page', async ({ page }) => {
    await page.getByLabel('Naam', { exact: true }).fill('Test Gebruiker');
    await page.getByLabel('Salonnaam').fill('Test Salon');
    await page.getByLabel('E-mailadres').fill('test@salon.nl');
    await page.getByRole('button', { name: 'Versturen' }).click();

    await expect(page).toHaveURL(/\/bevestiging\?type=demo/);
    await expect(page.getByText('Bedankt voor uw aanvraag!')).toBeVisible();
  });

  test('should show validation errors for empty required fields', async ({ page }) => {
    await page.getByRole('button', { name: 'Versturen' }).click();

    await expect(page.getByText('Naam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('Salonnaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('E-mailadres is verplicht.', { exact: true })).toBeVisible();
  });
});
