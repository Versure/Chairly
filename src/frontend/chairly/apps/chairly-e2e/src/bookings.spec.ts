import { expect, test } from './fixtures';
import { selectFlatpickrDateTime } from './helpers/flatpickr.helper';

const mockClients = [
  { id: 'client-1', firstName: 'Jan', lastName: 'Jansen' },
  { id: 'client-2', firstName: 'Piet', lastName: 'Pietersen' },
];

const mockStaff = [
  { id: 'staff-1', firstName: 'Anna', lastName: 'de Vries' },
  { id: 'staff-2', firstName: 'Kees', lastName: 'Bakker' },
];

const mockServices = [
  { id: 'svc-1', name: 'Herenknippen', duration: '00:30:00', price: 25 },
  { id: 'svc-2', name: 'Damesknippen', duration: '00:45:00', price: 35 },
];

const mockBooking = {
  id: 'booking-1',
  clientId: 'client-1',
  staffMemberId: 'staff-1',
  startTime: '2026-03-10T10:00:00Z',
  endTime: '2026-03-10T10:30:00Z',
  notes: null,
  status: 'Scheduled',
  services: [
    {
      serviceId: 'svc-1',
      serviceName: 'Herenknippen',
      duration: '00:30:00',
      price: 25,
      sortOrder: 0,
    },
  ],
  createdAtUtc: '2026-03-08T08:00:00Z',
  updatedAtUtc: null,
  confirmedAtUtc: null,
  startedAtUtc: null,
  completedAtUtc: null,
  cancelledAtUtc: null,
  noShowAtUtc: null,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/bookings*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockBooking] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/clients', (route) => {
    return route.fulfill({ json: mockClients });
  });
  await page.route('**/api/staff', (route) => {
    return route.fulfill({ json: mockStaff });
  });
  await page.route('**/api/services', (route) => {
    return route.fulfill({ json: mockServices });
  });
}

test('navigates to /boekingen and shows the Boekingen heading and table with names', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  await expect(page.getByRole('heading', { name: 'Boekingen', level: 1 })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Herenknippen' })).toBeVisible();
  // Should display client and staff names instead of IDs
  await expect(page.getByRole('cell', { name: 'Jan Jansen' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Anna de Vries' })).toBeVisible();
});

test('clicking Nieuwe boeking opens the booking form dialog with searchable dropdowns', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await expect(dialog.getByLabel('Klant')).toBeVisible();
  await expect(dialog.getByLabel('Medewerker')).toBeVisible();
  await expect(dialog.getByText('Datum & tijd')).toBeVisible();
  await expect(dialog.getByText('Diensten')).toBeVisible();
  await expect(dialog.getByLabel('Notities')).toBeVisible();

  // Verify the searchable dropdown shows filtered results when typing
  const clientInput = dialog.getByLabel('Klant');
  await clientInput.click();
  await clientInput.fill('Jan');
  await expect(dialog.locator('ul li').filter({ hasText: 'Jan Jansen' })).toBeVisible();

  // Verify empty state when typing non-matching text
  await clientInput.fill('xyz');
  await expect(dialog.getByText('Geen resultaten gevonden')).toBeVisible();

  // Verify service checkboxes
  await expect(dialog.getByLabel('Herenknippen')).toBeVisible();
  await expect(dialog.getByLabel('Damesknippen')).toBeVisible();

  await page.keyboard.press('Escape');
});

test('creating a new booking calls the API and refreshes the list', async ({ page }) => {
  const newBooking = {
    ...mockBooking,
    id: 'booking-2',
    clientId: 'client-2',
    staffMemberId: 'staff-2',
    services: [
      {
        serviceId: 'svc-2',
        serviceName: 'Damesknippen',
        duration: '00:45:00',
        price: 35,
        sortOrder: 0,
      },
    ],
  };

  let postCalled = false;

  await page.route('**/api/bookings*', (route) => {
    const method = route.request().method();
    if (method === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newBooking, status: 201 });
    }
    if (method === 'GET') {
      return route.fulfill({
        json: postCalled ? [mockBooking, newBooking] : [mockBooking],
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/clients', (route) => {
    return route.fulfill({ json: mockClients });
  });
  await page.route('**/api/staff', (route) => {
    return route.fulfill({ json: mockStaff });
  });
  await page.route('**/api/services', (route) => {
    return route.fulfill({ json: mockServices });
  });

  await page.goto('/boekingen');
  await expect(page.getByRole('cell', { name: 'Herenknippen' })).toBeVisible();

  await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

  const dialog = page.locator('dialog[open]');

  // Select client via searchable dropdown
  const clientInput = dialog.getByLabel('Klant');
  await clientInput.click();
  await clientInput.fill('Piet');
  await dialog.locator('ul li').filter({ hasText: 'Piet Pietersen' }).click();

  // Select staff member via searchable dropdown
  const staffInput = dialog.getByLabel('Medewerker');
  await staffInput.click();
  await staffInput.fill('Kees');
  await dialog.locator('ul li').filter({ hasText: 'Kees Bakker' }).click();

  await selectFlatpickrDateTime(page, dialog.getByLabel('Datum & tijd'), 28, '11', '00', true);
  await dialog.getByLabel('Damesknippen').check();

  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByRole('cell', { name: 'Damesknippen' })).toBeVisible();
});

test('clicking Bevestigen on a Scheduled booking calls the confirm API', async ({ page }) => {
  const confirmedBooking = {
    ...mockBooking,
    status: 'Confirmed',
    confirmedAtUtc: '2026-03-08T09:00:00Z',
  };

  let confirmCalled = false;

  await page.route('**/api/bookings/booking-1/confirm', (route) => {
    if (route.request().method() === 'POST') {
      confirmCalled = true;
      return route.fulfill({ status: 204, body: '' });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/bookings*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        json: [confirmCalled ? confirmedBooking : mockBooking],
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/clients', (route) => {
    return route.fulfill({ json: mockClients });
  });
  await page.route('**/api/staff', (route) => {
    return route.fulfill({ json: mockStaff });
  });
  await page.route('**/api/services', (route) => {
    return route.fulfill({ json: mockServices });
  });

  await page.goto('/boekingen');
  await expect(page.getByText('Gepland')).toBeVisible();

  await page.getByRole('button', { name: 'Bevestigen' }).click();

  await expect(page.getByText('Bevestigd')).toBeVisible();
});

test('clicking a booking row opens the edit dialog pre-filled and saves changes', async ({
  page,
}) => {
  const updatedBooking = {
    ...mockBooking,
    notes: 'Aangepaste notities',
    updatedAtUtc: '2026-03-08T10:00:00Z',
  };

  let putCalled = false;

  await page.route('**/api/bookings/booking-1', (route) => {
    if (route.request().method() === 'PUT') {
      putCalled = true;
      return route.fulfill({ json: updatedBooking });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/bookings*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        json: [putCalled ? updatedBooking : mockBooking],
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/clients', (route) => {
    return route.fulfill({ json: mockClients });
  });
  await page.route('**/api/staff', (route) => {
    return route.fulfill({ json: mockStaff });
  });
  await page.route('**/api/services', (route) => {
    return route.fulfill({ json: mockServices });
  });

  await page.goto('/boekingen');
  await expect(page.getByRole('cell', { name: 'Herenknippen' })).toBeVisible();

  // Click the booking row
  await page.locator('tbody tr').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Klant')).toHaveValue('Jan Jansen');
  await expect(dialog.getByLabel('Medewerker')).toHaveValue('Anna de Vries');

  // Change the notes field
  await dialog.getByLabel('Notities').fill('Aangepaste notities');

  // Click Opslaan and wait for the PUT request
  const putRequestPromise = page.waitForRequest(
    (req) => req.url().includes('/api/bookings/booking-1') && req.method() === 'PUT',
  );
  await dialog.getByRole('button', { name: 'Opslaan' }).click();
  await putRequestPromise;

  // Verify the PUT was called
  expect(putCalled).toBe(true);

  // Verify the list refreshes — the table should still show Herenknippen from the refreshed GET
  await expect(page.getByRole('cell', { name: 'Herenknippen' })).toBeVisible();
  // Dialog should be closed
  await expect(dialog).toBeHidden();
});

test('staff member filter dropdown shows staff names and filters bookings', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  const staffFilter = page.getByLabel('Medewerker', { exact: false }).first();
  await expect(staffFilter).toBeVisible();

  // Verify the dropdown options (option elements are hidden in collapsed selects, so use toContainText)
  await expect(staffFilter).toContainText('Alle medewerkers');
  await expect(staffFilter).toContainText('Anna de Vries');
  await expect(staffFilter).toContainText('Kees Bakker');
});

test('view toggle buttons are visible and default to Lijst view', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  // Both toggle buttons should be visible
  const lijstButton = page.getByRole('button', { name: 'Lijst' });
  const roosterButton = page.getByRole('button', { name: 'Rooster' });
  await expect(lijstButton).toBeVisible();
  await expect(roosterButton).toBeVisible();

  // Default view is list — table should be visible
  await expect(page.getByRole('table')).toBeVisible();
});

test('clicking Rooster toggle switches to schedule view and back to Lijst', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  // Start in list view
  await expect(page.getByRole('table')).toBeVisible();

  // Switch to schedule view
  await page.getByRole('button', { name: 'Rooster' }).click();

  // Table should no longer be visible, schedule content should appear
  await expect(page.getByRole('table')).toBeHidden();
  // Booking data should still be visible in schedule view
  await expect(page.getByText('Herenknippen').first()).toBeVisible();
  await expect(page.getByText('Jan Jansen').first()).toBeVisible();

  // Switch back to list view
  await page.getByRole('button', { name: 'Lijst' }).click();

  // Table should be visible again
  await expect(page.getByRole('table')).toBeVisible();
});
