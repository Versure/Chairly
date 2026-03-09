import { expect, test } from './fixtures';

test.describe('Collapsible sidebar navigation', () => {
  test('desktop: sidebar visible by default', async ({ page }) => {
    await page.goto('/');

    const sidebar = page.locator('nav');
    await expect(sidebar).toBeVisible();

    await expect(page.getByRole('link', { name: 'Diensten' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Klanten' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Medewerkers' })).toBeVisible();

    const hamburger = page.getByRole('button', { name: /Menu openen|Menu sluiten/ });
    await expect(hamburger).toBeHidden();
  });

  test('mobile: sidebar hidden by default', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    const sidebar = page.locator('nav');
    await expect(sidebar).not.toBeInViewport();

    const hamburger = page.getByRole('button', { name: 'Menu openen' });
    await expect(hamburger).toBeVisible();
  });

  test('mobile: hamburger opens sidebar', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    await page.getByRole('button', { name: 'Menu openen' }).click();

    const sidebar = page.locator('nav');
    await expect(sidebar).toBeInViewport();

    await expect(page.getByRole('link', { name: 'Diensten' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Klanten' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Medewerkers' })).toBeVisible();

    const backdrop = page.locator('.fixed.inset-0.bg-black\\/50');
    await expect(backdrop).toBeVisible();
  });

  test('mobile: clicking a nav link closes sidebar', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    await page.getByRole('button', { name: 'Menu openen' }).click();

    const sidebar = page.locator('nav');
    await expect(sidebar).toBeInViewport();

    await page.getByRole('link', { name: 'Diensten' }).click();

    await expect(sidebar).not.toBeInViewport();
  });

  test('mobile: clicking backdrop closes sidebar', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    await page.getByRole('button', { name: 'Menu openen' }).click();

    const sidebar = page.locator('nav');
    await expect(sidebar).toBeInViewport();

    const backdrop = page.locator('.fixed.inset-0.bg-black\\/50');
    await backdrop.click();

    await expect(sidebar).not.toBeInViewport();
  });
});
