import { expect, test } from './fixtures';

async function setupAllApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/staff', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/clients', (route) => route.fulfill({ json: [] }));
}

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
    // Click to the right of the sidebar (w-60 = 240px) to avoid pointer interception
    await backdrop.click({ position: { x: 300, y: 300 } });

    await expect(sidebar).not.toBeInViewport();
  });
});

test.describe('Cross-cutting navigation and theme', () => {
  test('navigating to / redirects to /diensten', async ({ page }) => {
    await setupAllApiMocks(page);
    await page.goto('/');

    await expect(page).toHaveURL(/\/diensten/);
  });

  test('all nav links navigate to correct pages', async ({ page }) => {
    await setupAllApiMocks(page);
    await page.goto('/diensten');

    await page.getByRole('link', { name: 'Klanten' }).click();
    await expect(page).toHaveURL(/\/klanten/);

    await page.getByRole('link', { name: 'Medewerkers' }).click();
    await expect(page).toHaveURL(/\/medewerkers/);

    await page.getByRole('link', { name: 'Diensten' }).click();
    await expect(page).toHaveURL(/\/diensten/);
  });

  test('each page shows the correct h1 heading', async ({ page }) => {
    await setupAllApiMocks(page);

    await page.goto('/diensten');
    await expect(page.getByRole('heading', { name: 'Diensten', level: 1 })).toBeVisible();

    await page.goto('/klanten');
    await expect(page.getByRole('heading', { name: 'Klanten', level: 1 })).toBeVisible();

    await page.goto('/medewerkers');
    await expect(page.getByRole('heading', { name: 'Medewerkers', level: 1 })).toBeVisible();
  });

  test('theme toggle switches between light and dark mode', async ({ page }) => {
    await setupAllApiMocks(page);
    await page.goto('/diensten');

    // Initially in light mode — button should say "Schakel naar donker thema"
    const toggleButton = page.getByRole('button', { name: 'Schakel naar donker thema' });
    await expect(toggleButton).toBeVisible();

    // Click to switch to dark mode
    await toggleButton.click();

    // After clicking, data-theme should be "dark"
    await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark');

    // Button label should now say "Schakel naar licht thema"
    const lightToggle = page.getByRole('button', { name: 'Schakel naar licht thema' });
    await expect(lightToggle).toBeVisible();

    // Click to switch back to light mode
    await lightToggle.click();

    // data-theme should be removed or set to "light"
    await expect(page.locator('html')).not.toHaveAttribute('data-theme', 'dark');
  });

  test('active nav link is highlighted with bg-primary-600 class', async ({ page }) => {
    await setupAllApiMocks(page);

    await page.goto('/diensten');

    // The active link should have bg-primary-600 from routerLinkActive
    const dienstenLink = page.getByRole('link', { name: 'Diensten' });
    await expect(dienstenLink).toHaveClass(/(?:^|\s)bg-primary-600(?:\s|$)/);

    // Navigate to klanten
    await page.getByRole('link', { name: 'Klanten' }).click();
    await expect(page).toHaveURL(/\/klanten/);

    // Now Klanten link should be active and Diensten should not
    const klantenLink = page.getByRole('link', { name: 'Klanten' });
    await expect(klantenLink).toHaveClass(/(?:^|\s)bg-primary-600(?:\s|$)/);
    await expect(dienstenLink).not.toHaveClass(/(?:^|\s)bg-primary-600(?:\s|$)/);
  });
});
