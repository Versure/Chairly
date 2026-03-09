import { expect, test } from './fixtures';

const mockClient = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockClient] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigating to /klanten shows h1 Klanten and a table row for the mocked client', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/klanten');

  await expect(page.getByRole('heading', { name: 'Klanten', level: 1 })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByText('Bakker, Anna')).toBeVisible();
});

test('mocked client shows formatted name as "lastName, firstName" in the table', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/klanten');

  await expect(page.getByText('Bakker, Anna')).toBeVisible();
});

test('clicking + Klant toevoegen opens the form dialog', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten');

  await page.getByRole('button', { name: '+ Klant toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await page.keyboard.press('Escape');
});

test('filling in Voornaam + Achternaam and clicking Opslaan calls POST /api/clients and the new client appears in the table', async ({
  page,
}) => {
  const newClient = {
    ...mockClient,
    id: 'client-2',
    firstName: 'Bert',
    lastName: 'Claassen',
    email: null,
  };

  let postCalled = false;

  await page.route('**/api/clients', (route) => {
    const method = route.request().method();
    if (method === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newClient });
    }
    if (method === 'GET') {
      return route.fulfill({ json: [mockClient] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/klanten');
  await expect(page.getByText('Bakker, Anna')).toBeVisible();

  await page.getByRole('button', { name: '+ Klant toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Voornaam').fill('Bert');
  await dialog.getByLabel('Achternaam').fill('Claassen');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  expect(postCalled).toBe(true);
  await expect(page.getByText('Claassen, Bert')).toBeVisible();
});

test('clicking button with title Klant bewerken opens the dialog pre-filled with the client Voornaam', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/klanten');

  await expect(page.getByText('Bakker, Anna')).toBeVisible();

  await page.locator('button[title="Klant bewerken"]').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Voornaam')).toHaveValue('Anna');

  await page.keyboard.press('Escape');
});

test('clicking button with title Klant verwijderen shows the confirmation dialog and confirms delete', async ({
  page,
}) => {
  let deleteCalled = false;

  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockClient] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/clients/client-1', (route) => {
    deleteCalled = true;
    return route.fulfill({ status: 204, body: '' });
  });

  await page.goto('/klanten');
  await expect(page.getByText('Bakker, Anna')).toBeVisible();

  await page.locator('button[title="Klant verwijderen"]').first().click();

  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();
  await confirmDialog.getByRole('button', { name: 'Verwijderen' }).click();

  expect(deleteCalled).toBe(true);
  await expect(page.getByText('Bakker, Anna')).not.toBeVisible();
});

test('Klanten link is visible in the sidebar nav and clicking it navigates to /klanten', async ({
  page,
}) => {
  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/staff', (route) => route.fulfill({ json: [] }));

  await page.goto('/diensten');

  const navLink = page.getByRole('link', { name: 'Klanten' });
  await expect(navLink).toBeVisible();

  await navLink.click();
  await expect(page).toHaveURL(/\/klanten/);
});
