import { expect, test } from '@playwright/test';

const mockPlans = [
  {
    slug: 'starter',
    name: 'Starter',
    maxStaff: 1,
    monthlyPrice: 14.99,
    annualPricePerMonth: 13.49,
  },
  {
    slug: 'team',
    name: 'Team',
    maxStaff: 5,
    monthlyPrice: 59.99,
    annualPricePerMonth: 53.99,
  },
  {
    slug: 'salon',
    name: 'Salon',
    maxStaff: 15,
    monthlyPrice: 149.0,
    annualPricePerMonth: 134.1,
  },
];

const mockSubscriptionResponse = {
  id: '00000000-0000-0000-0000-000000000001',
  salonName: 'Test Salon',
  ownerFirstName: 'Jan',
  ownerLastName: 'Jansen',
  email: 'jan@salon.nl',
  plan: 'starter',
  billingCycle: null,
  isTrial: true,
  trialEndsAtUtc: '2026-04-20T00:00:00Z',
  createdAtUtc: '2026-03-21T00:00:00Z',
};

test.describe('Subscribe Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockPlans),
      }),
    );

    await page.route('**/api/onboarding/subscriptions', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(mockSubscriptionResponse),
        });
      }
      return route.continue();
    });
  });

  test('should display trial heading', async ({ page }) => {
    await page.goto('/abonneren?plan=starter&trial=true');
    await expect(page.getByRole('heading', { name: 'Start uw gratis proefperiode' })).toBeVisible();
  });

  test('should display paid heading', async ({ page }) => {
    await page.goto('/abonneren?plan=team&trial=false&cyclus=monthly');
    await expect(page.getByRole('heading', { name: 'Uw gegevens' })).toBeVisible();
  });

  test('should display plan summary with Team plan', async ({ page }) => {
    await page.goto('/abonneren?plan=team&trial=false&cyclus=monthly');
    await expect(page.getByText('Gekozen plan: Team')).toBeVisible();
  });

  test('should submit trial form and navigate to confirmation', async ({ page }) => {
    await page.goto('/abonneren?plan=starter&trial=true');
    await page.getByLabel('Salonnaam').fill('Test Salon');
    await page.getByLabel('Voornaam').fill('Jan');
    await page.getByLabel('Achternaam').fill('Jansen');
    await page.getByLabel('E-mailadres').fill('jan@salon.nl');
    await page.getByRole('button', { name: 'Proefperiode starten' }).click();

    await expect(page).toHaveURL(/\/bevestiging\?type=abonnement/);
    await expect(page.getByText('Bedankt voor uw aanmelding!')).toBeVisible();
  });

  test('should show validation errors for empty required fields', async ({ page }) => {
    await page.goto('/abonneren?plan=starter&trial=true');
    await page.getByRole('button', { name: 'Proefperiode starten' }).click();

    await expect(page.getByText('Salonnaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('Voornaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('Achternaam is verplicht.', { exact: true })).toBeVisible();
    await expect(page.getByText('E-mailadres is verplicht.', { exact: true })).toBeVisible();
  });

  test('should redirect to pricing when no plan query param', async ({ page }) => {
    await page.goto('/abonneren');
    await expect(page).toHaveURL(/\/prijzen/);
  });
});
