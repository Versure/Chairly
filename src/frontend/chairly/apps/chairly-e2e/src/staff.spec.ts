import { expect, test } from '@playwright/test';

const mockStaffMember = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
  role: 'staff_member',
  color: '#6366f1',
  photoUrl: null,
  isActive: true,
  schedule: {},
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/staff', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockStaffMember] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigating to /medewerkers shows h1 Medewerkers and a table row for the mocked staff member', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await expect(page.getByRole('heading', { name: 'Medewerkers', level: 1 })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByText('Jan Jansen')).toBeVisible();
});

test('mocked staff member shows full name, role label Medewerker, and status badge Actief', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await expect(page.getByText('Jan Jansen')).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Medewerker', exact: true })).toBeVisible();
  await expect(page.getByText('Actief')).toBeVisible();
});

test('clicking + Medewerker toevoegen opens the form dialog', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await page.getByRole('button', { name: '+ Medewerker toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await page.keyboard.press('Escape');
});

test('filling in the form and clicking Opslaan calls POST /api/staff and shows new member in table', async ({
  page,
}) => {
  const newStaffMember = {
    ...mockStaffMember,
    id: 'staff-2',
    firstName: 'Piet',
    lastName: 'Pietersen',
  };

  let postCalled = false;

  await page.route('**/api/staff', (route) => {
    const method = route.request().method();
    if (method === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newStaffMember });
    }
    if (method === 'GET') {
      return route.fulfill({ json: [mockStaffMember] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/medewerkers');
  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.getByRole('button', { name: '+ Medewerker toevoegen' }).click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Voornaam').fill('Piet');
  await dialog.getByLabel('Achternaam').fill('Pietersen');
  await dialog.getByLabel('Rol').selectOption('staff_member');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  expect(postCalled).toBe(true);
  await expect(page.getByText('Piet Pietersen')).toBeVisible();
});

test('clicking Medewerker bewerken opens the dialog pre-filled with the staff member first name', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.locator('button[title="Medewerker bewerken"]').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Voornaam')).toHaveValue('Jan');

  await page.keyboard.press('Escape');
});

test('clicking Medewerker deactiveren shows confirmation; confirming calls PATCH deactivate and member shows Inactief badge', async ({
  page,
}) => {
  let patchCalled = false;

  await page.route('**/api/staff', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockStaffMember] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/staff/staff-1/deactivate', (route) => {
    if (route.request().method() === 'PATCH') {
      patchCalled = true;
      return route.fulfill({ status: 204, body: '' });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/medewerkers');
  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.locator('button[title="Medewerker deactiveren"]').first().click();

  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();
  await confirmDialog.getByRole('button', { name: 'Deactiveren' }).click();

  expect(patchCalled).toBe(true);
  await expect(page.getByText('Inactief')).toBeVisible();
});

test('Medewerkers nav link is visible in the sidebar and clicking it navigates to /medewerkers', async ({
  page,
}) => {
  await page.route('**/api/staff', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));

  await page.goto('/diensten');

  const navLink = page.getByRole('link', { name: 'Medewerkers' });
  await expect(navLink).toBeVisible();

  await navLink.click();
  await expect(page).toHaveURL(/\/medewerkers/);
});
