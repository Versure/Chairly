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

  test('should navigate to demo page when clicking Demo aanvragen CTA', async ({ page }) => {
    await page.locator('chairly-web-hero-section').getByText('Demo aanvragen').click();
    await expect(page).toHaveURL(/\/demo-aanvragen/);
  });

  test('should navigate to sign-up page when clicking Aanmelden CTA', async ({ page }) => {
    await page.locator('chairly-web-hero-section').getByText('Nu aanmelden').click();
    await expect(page).toHaveURL(/\/aanmelden/);
  });

  test('should render header with navigation links', async ({ page }) => {
    const header = page.locator('chairly-web-header');
    await expect(header.getByText('Chairly')).toBeVisible();
  });

  test('should render footer with copyright text', async ({ page }) => {
    const footer = page.locator('chairly-web-footer');
    await expect(footer.getByText('2026 Chairly')).toBeVisible();
  });
});
