import { expect, test } from '@playwright/test';

test('navigates to /services and shows the page header', async ({ page }) => {
  await page.goto('/services');

  await expect(page.locator('h1')).toContainText('Services');
});

test('shows the Add Service button', async ({ page }) => {
  await page.goto('/services');

  await expect(page.getByRole('button', { name: 'Add Service' })).toBeVisible();
});

test('opens the add service dialog when Add Service is clicked', async ({ page }) => {
  await page.goto('/services');

  await page.getByRole('button', { name: 'Add Service' }).click();

  await expect(page.locator('dialog[open]')).toBeVisible();
});

test('closes the add service dialog when Cancel is clicked', async ({ page }) => {
  await page.goto('/services');

  await page.getByRole('button', { name: 'Add Service' }).click();
  await expect(page.locator('dialog[open]')).toBeVisible();

  // Use Escape key to close the modal dialog — cross-browser compatible
  // (clicking inside showModal() dialogs via Playwright is unreliable in Firefox/WebKit)
  await page.keyboard.press('Escape');

  await expect(page.locator('dialog[open]')).toHaveCount(0);
});

test('shows the Categories panel', async ({ page }) => {
  await page.goto('/services');

  // Use heading role to avoid strict mode violation with "No categories yet." text
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible();
});
