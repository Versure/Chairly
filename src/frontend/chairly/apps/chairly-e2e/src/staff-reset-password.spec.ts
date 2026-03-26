import { expect, test } from './fixtures';

const mockStaffMember = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
  email: 'jan.jansen@salon.nl',
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

  await page.route('**/api/staff/staff-1/reset-password', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 200, body: '' });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('manager can trigger password reset from table and sees success banner', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.locator('button[title="Wachtwoord resetten"]').first().click();

  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();
  await expect(confirmDialog.getByText('Wachtwoord resetten')).toBeVisible();
  await confirmDialog.getByRole('button', { name: 'Versturen' }).click();

  await expect(
    page.getByText('Wachtwoord-reset e-mail is verstuurd naar Jan Jansen.'),
  ).toBeVisible();
});

test('manager can trigger password reset from edit dialog and sees success banner', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/medewerkers');

  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.locator('button[title="Medewerker bewerken"]').first().click();

  const editDialog = page.locator('dialog[open]');
  await expect(editDialog).toBeVisible();

  await editDialog.getByRole('button', { name: 'Reset wachtwoord' }).click();

  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();
  await confirmDialog.getByRole('button', { name: 'Versturen' }).click();

  await expect(
    page.getByText('Wachtwoord-reset e-mail is verstuurd naar Jan Jansen.'),
  ).toBeVisible();
});

test('confirmation dialog can be cancelled without sending reset email', async ({ page }) => {
  let resetCalled = false;

  await page.route('**/api/staff', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockStaffMember] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/staff/staff-1/reset-password', (route) => {
    if (route.request().method() === 'POST') {
      resetCalled = true;
      return route.fulfill({ status: 200, body: '' });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/medewerkers');
  await expect(page.getByText('Jan Jansen')).toBeVisible();

  await page.locator('button[title="Wachtwoord resetten"]').first().click();

  const confirmDialog = page.locator('dialog[open]');
  await expect(confirmDialog).toBeVisible();

  await page.keyboard.press('Escape');

  expect(resetCalled).toBe(false);
  await expect(
    page.getByText('Wachtwoord-reset e-mail is verstuurd naar Jan Jansen.'),
  ).toBeHidden();
});
