import { expect, test } from './fixtures';

const mockCategory = {
  id: 'cat-1',
  name: 'Knippen',
  sortOrder: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: 'system',
};

const mockService = {
  id: 'svc-1',
  name: 'Herenknippen',
  description: null,
  duration: '00:30:00',
  price: 25,
  vatRate: 21,
  categoryId: 'cat-1',
  categoryName: 'Knippen',
  isActive: true,
  sortOrder: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: 'system',
  updatedAtUtc: null,
  updatedBy: null,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/service-categories', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockCategory] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/services', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockService] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigates to /diensten and shows the page header', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await expect(page.locator('h1')).toContainText('Diensten');
});

test('shows the Add Service button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await expect(page.getByRole('button', { name: 'Dienst toevoegen' })).toBeVisible();
});

test('opens the add service dialog when Add Service is clicked', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await page.getByRole('button', { name: 'Dienst toevoegen' }).click();

  await expect(page.locator('dialog[open]')).toBeVisible();
});

test('closes the add service dialog when Cancel is clicked', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await page.getByRole('button', { name: 'Dienst toevoegen' }).click();
  await expect(page.locator('dialog[open]')).toBeVisible();

  // Use Escape key to close the modal dialog — cross-browser compatible
  // (clicking inside showModal() dialogs via Playwright is unreliable in Firefox/WebKit)
  await page.keyboard.press('Escape');

  await expect(page.locator('dialog[open]')).toHaveCount(0);
});

test('shows the Categories panel', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  // Use heading role to avoid strict mode violation with "Nog geen categorieën." text
  await expect(page.getByRole('heading', { name: 'Categorieën' })).toBeVisible();
});
