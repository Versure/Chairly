import { expect, test } from '@playwright/test';

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

test('navigates to /diensten and shows the Diensten heading and services table', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await expect(page.getByRole('heading', { name: 'Diensten', level: 1 })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByText('Herenknippen')).toBeVisible();
});

test('clicking Dienst toevoegen opens a centered dialog with service form fields', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await page.getByRole('button', { name: 'Dienst toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await expect(dialog.getByLabel('Naam')).toBeVisible();
  await expect(dialog.getByLabel('Omschrijving')).toBeVisible();
  await expect(dialog.getByLabel('Duur (minuten)')).toBeVisible();
  await expect(dialog.getByLabel('Prijs')).toBeVisible();
  await expect(dialog.getByLabel('Categorie')).toBeVisible();

  await page.keyboard.press('Escape');
});

test('filling in the form and clicking Opslaan calls the API and shows the new service in the table', async ({ page }) => {
  const newService = {
    ...mockService,
    id: 'svc-2',
    name: 'Damesknippen',
    price: 35,
    categoryName: 'Knippen',
  };

  let postCalled = false;

  await page.route('**/api/service-categories', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockCategory] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/services', (route) => {
    const method = route.request().method();
    if (method === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newService });
    }
    if (method === 'GET') {
      return route.fulfill({ json: postCalled ? [mockService, newService] : [mockService] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/diensten');
  await expect(page.getByText('Herenknippen')).toBeVisible();

  await page.getByRole('button', { name: 'Dienst toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Naam').fill('Damesknippen');
  await dialog.getByLabel('Duur (minuten)').fill('45');
  await dialog.getByLabel('Prijs').fill('35');

  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Damesknippen')).toBeVisible();
});

test('clicking Bewerken on a service row opens the form dialog pre-filled', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await expect(page.getByText('Herenknippen')).toBeVisible();

  await page.locator('button[title="Dienst bewerken"]').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Naam')).toHaveValue('Herenknippen');
  await expect(dialog.getByLabel('Prijs')).toHaveValue('25');

  await page.keyboard.press('Escape');
});

test('clicking Verwijderen shows the confirmation dialog and removes the service on confirm', async ({ page }) => {
  let deleteCount = 0;

  await page.route('**/api/service-categories', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockCategory] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/services/svc-1', (route) => {
    if (route.request().method() === 'DELETE') {
      return route.fulfill({ status: 204, body: '' });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/services', (route) => {
    const method = route.request().method();
    if (method === 'GET') {
      deleteCount++;
      return route.fulfill({ json: deleteCount > 1 ? [] : [mockService] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/diensten');
  await expect(page.getByText('Herenknippen')).toBeVisible();

  await page.locator('button[title="Dienst verwijderen"]').first().click();

  // Confirmation dialog should appear
  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();

  // Click the destructive confirm button (Verwijderen in the dialog)
  await confirmDialog.getByRole('button', { name: 'Verwijderen' }).click();

  await expect(page.getByText('Herenknippen')).toBeHidden();
});

test('category panel shows the Categorieën header and list of categories', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  // Wait for services to load to ensure the page is fully rendered
  await expect(page.getByText('Herenknippen')).toBeVisible();

  await expect(page.getByRole('heading', { name: 'Categorieën' })).toBeVisible();
  await expect(page.locator('chairly-category-panel').getByText('Knippen')).toBeVisible();
});

test('clicking + Toevoegen in the category panel shows an input field', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/diensten');

  await page.getByRole('button', { name: '+ Toevoegen' }).click();

  await expect(page.getByPlaceholder('Naam categorie')).toBeVisible();
});
