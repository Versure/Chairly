import { expect, test } from '@playwright/test';

test.describe('Landing Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display hero section heading', async ({ page }) => {
    await expect(page.getByText('De salon software die voor u werkt')).toBeVisible();
  });

  test('should render at least 4 feature cards', async ({ page }) => {
    const cards = page.locator('chairly-web-feature-card');
    await expect(cards).toHaveCount(4);
  });

  test('should show "Gratis proberen" primary CTA in hero section', async ({ page }) => {
    await expect(
      page.locator('chairly-web-hero-section').getByText('Gratis proberen'),
    ).toBeVisible();
  });

  test('should navigate to subscribe page when clicking "Gratis proberen" CTA', async ({
    page,
  }) => {
    // Mock the plans API for the subscribe page
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            slug: 'starter',
            name: 'Starter',
            maxStaff: 1,
            monthlyPrice: 14.99,
            annualPricePerMonth: 13.49,
          },
        ]),
      }),
    );

    await page.locator('chairly-web-hero-section').getByText('Gratis proberen').click();
    await expect(page).toHaveURL(/\/abonneren\?plan=starter&trial=true/);
  });

  test('should navigate to pricing page when clicking "Bekijk prijzen" CTA', async ({ page }) => {
    // Mock the plans API for the pricing page
    await page.route('**/api/onboarding/plans', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      }),
    );

    await page.locator('chairly-web-hero-section').getByText('Bekijk prijzen').click();
    await expect(page).toHaveURL(/\/prijzen/);
  });

  test('should render pricing summary section on landing page', async ({ page }) => {
    await expect(page.getByText('Eenvoudige, transparante prijzen')).toBeVisible();
  });

  test('should render header with "Prijzen" and "Gratis proberen" links', async ({ page }) => {
    const header = page.locator('chairly-web-header');
    await expect(header.getByText('Chairly')).toBeVisible();
    await expect(header.getByText('Prijzen')).toBeVisible();
    await expect(header.getByText('Gratis proberen')).toBeVisible();
  });

  test('should render footer with copyright text', async ({ page }) => {
    const footer = page.locator('chairly-web-footer');
    await expect(footer.getByText('2026 Chairly')).toBeVisible();
  });
});
