import { expect, test } from '@playwright/test';

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
  await page.route('**/api/bookings', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockBooking] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigates to /boekingen and shows the Boekingen heading and table', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  await expect(page.getByRole('heading', { name: 'Boekingen', level: 1 })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByText('Herenknippen')).toBeVisible();
});

test('clicking Nieuwe boeking opens the booking form dialog', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/boekingen');

  await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await expect(dialog.getByLabel('Klant ID')).toBeVisible();
  await expect(dialog.getByLabel('Medewerker ID')).toBeVisible();
  await expect(dialog.getByLabel('Datum & tijd')).toBeVisible();
  await expect(dialog.getByLabel('Dienst-IDs (komma-gescheiden)')).toBeVisible();
  await expect(dialog.getByLabel('Notities')).toBeVisible();

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

  await page.route('**/api/bookings', (route) => {
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

  await page.goto('/boekingen');
  await expect(page.getByText('Herenknippen')).toBeVisible();

  await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Klant ID').fill('client-2');
  await dialog.getByLabel('Medewerker ID').fill('staff-2');
  await dialog.getByLabel('Datum & tijd').fill('2026-03-10T11:00');
  await dialog.getByLabel('Dienst-IDs (komma-gescheiden)').fill('svc-2');

  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Damesknippen')).toBeVisible();
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

  await page.route('**/api/bookings', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        json: [confirmCalled ? confirmedBooking : mockBooking],
      });
    }
    return route.fulfill({ status: 404, body: '' });
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

  await page.route('**/api/bookings', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        json: [putCalled ? updatedBooking : mockBooking],
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/boekingen');
  await expect(page.getByText('Herenknippen')).toBeVisible();

  // Click the booking row
  await page.locator('tbody tr').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Klant ID')).toHaveValue('client-1');
  await expect(dialog.getByLabel('Medewerker ID')).toHaveValue('staff-1');

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
  await expect(page.getByText('Herenknippen')).toBeVisible();
  // Dialog should be closed
  await expect(dialog).toBeHidden();
});
