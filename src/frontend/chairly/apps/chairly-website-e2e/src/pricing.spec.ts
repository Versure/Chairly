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

test.describe('Pricing Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockPlans),
      }),
    );

    await page.goto('/prijzen');
  });

  test('should display heading', async ({ page }) => {
    await expect(
      page.getByRole('heading', { name: 'Kies het plan dat bij uw salon past' }),
    ).toBeVisible();
  });

  test('should display 4 plan cards (trial + 3 paid)', async ({ page }) => {
    const cards = page.locator('chairly-web-pricing-card');
    await expect(cards).toHaveCount(4);
  });

  test('should display billing cycle toggle', async ({ page }) => {
    await expect(page.getByText('Maandelijks')).toBeVisible();
    await expect(page.getByText('Jaarlijks (10% korting)')).toBeVisible();
  });

  test('should switch displayed prices when toggling billing cycle', async ({ page }) => {
    // Default is monthly — Starter card (second card, after trial) should show monthly price
    const starterCard = page.locator('chairly-web-pricing-card').nth(1);
    await expect(starterCard.getByText('14,99')).toBeVisible();

    // Click annual toggle
    await page.getByText('Jaarlijks (10% korting)').click();

    // Starter card should now show annual price per month
    await expect(starterCard.getByText('13,49')).toBeVisible();
    // Monthly price should no longer be visible on the Starter card
    await expect(starterCard.getByText('14,99')).toBeHidden();
  });

  test('should navigate to subscribe page when clicking trial "Gratis starten"', async ({
    page,
  }) => {
    // Also mock plans for the subscribe page
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockPlans),
      }),
    );

    const trialCard = page.locator('chairly-web-pricing-card').first();
    await trialCard.getByText('Gratis starten').click();
    await expect(page).toHaveURL(/\/abonneren\?plan=starter&trial=true/);
  });

  test('should navigate to subscribe page when clicking paid plan CTA', async ({ page }) => {
    // Also mock plans for the subscribe page
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockPlans),
      }),
    );

    // Click on second card (Starter paid plan)
    const starterCard = page.locator('chairly-web-pricing-card').nth(1);
    await starterCard.getByText('Abonnement kiezen').click();
    await expect(page).toHaveURL(/\/abonneren\?plan=starter&trial=false&cyclus=monthly/);
  });

  test('should display feature comparison table', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Vergelijk plannen' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Boekingen beheren' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'Aantal medewerkers' })).toBeVisible();
  });

  test('should display FAQ section', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Veelgestelde vragen' })).toBeVisible();
    await expect(page.getByText('Hoe werkt de gratis proefperiode?')).toBeVisible();
  });
});
