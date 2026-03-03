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

  await page.locator('dialog[open]').getByRole('button', { name: 'Cancel' }).click();

  await expect(page.locator('dialog[open]')).toHaveCount(0);
});

test('shows the Categories panel', async ({ page }) => {
  await page.goto('/services');

  await expect(page.getByText('Categories')).toBeVisible();
});
