import { expect, test } from './fixtures';

// Bookings feature is not yet implemented (no /boekingen route exists).
// All tests are wrapped in test.describe.skip and should be enabled once the bookings feature is built.
// Mock data shapes are based on the domain model and should be refined once the bookings API contract is defined.

const mockClient = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
};

const mockStaffMember = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'Jansen',
};

const mockService = {
  id: 'svc-1',
  name: 'Herenknippen',
  duration: '00:30:00',
  price: 25,
};

const mockBooking = {
  id: 'booking-1',
  clientId: 'client-1',
  clientName: 'Anna Bakker',
  staffMemberId: 'staff-1',
  staffMemberName: 'Jan Jansen',
  startTime: '2026-03-10T10:00:00Z',
  endTime: '2026-03-10T10:30:00Z',
  notes: null,
  services: [
    {
      serviceId: 'svc-1',
      serviceName: 'Herenknippen',
      duration: '00:30:00',
      price: 25,
      sortOrder: 0,
    },
  ],
  createdAtUtc: '2026-03-09T00:00:00Z',
  createdBy: 'system',
  confirmedAtUtc: null,
  startedAtUtc: null,
  completedAtUtc: null,
  cancelledAtUtc: null,
};

test.describe.skip('Boekingen (pending bookings feature)', () => {
  test('bookings list page loads and shows Boekingen heading', async ({ page }) => {
    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await expect(page.getByRole('heading', { name: 'Boekingen', level: 1 })).toBeVisible();
  });

  test('clicking Nieuwe boeking opens the create dialog', async ({ page }) => {
    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

    const dialog = page.locator('dialog[open]');
    await expect(dialog).toBeVisible();

    await page.keyboard.press('Escape');
  });

  test('creating a booking shows it in the list', async ({ page }) => {
    let postCalled = false;

    await page.route('**/api/clients', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockClient] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/staff', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockStaffMember] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/services', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockService] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/bookings', (route) => {
      const method = route.request().method();
      if (method === 'POST') {
        postCalled = true;
        return route.fulfill({ json: mockBooking });
      }
      if (method === 'GET') {
        return route.fulfill({ json: postCalled ? [mockBooking] : [] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Nieuwe boeking' }).click();

    // Fill in booking form fields (selectors to be refined once the UI is built)
    const dialog = page.locator('dialog[open]');
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Opslaan' }).click();

    expect(postCalled).toBe(true);
    await expect(page.getByText('Anna Bakker')).toBeVisible();
  });

  test('confirming a booking changes status badge to Bevestigd', async ({ page }) => {
    const confirmedBooking = {
      ...mockBooking,
      confirmedAtUtc: '2026-03-09T12:00:00Z',
    };

    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/bookings/booking-1/confirm', (route) => {
      if (route.request().method() === 'PATCH') {
        return route.fulfill({ json: confirmedBooking });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Bevestigen' }).first().click();

    await expect(page.getByText('Bevestigd')).toBeVisible();
  });

  test('starting a booking changes status badge to Bezig', async ({ page }) => {
    const confirmedBooking = {
      ...mockBooking,
      confirmedAtUtc: '2026-03-09T12:00:00Z',
    };

    const startedBooking = {
      ...confirmedBooking,
      startedAtUtc: '2026-03-10T10:00:00Z',
    };

    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [confirmedBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/bookings/booking-1/start', (route) => {
      if (route.request().method() === 'PATCH') {
        return route.fulfill({ json: startedBooking });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Starten' }).first().click();

    await expect(page.getByText('Bezig')).toBeVisible();
  });

  test('completing a booking changes status badge to Voltooid', async ({ page }) => {
    const startedBooking = {
      ...mockBooking,
      confirmedAtUtc: '2026-03-09T12:00:00Z',
      startedAtUtc: '2026-03-10T10:00:00Z',
    };

    const completedBooking = {
      ...startedBooking,
      completedAtUtc: '2026-03-10T10:30:00Z',
    };

    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [startedBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/bookings/booking-1/complete', (route) => {
      if (route.request().method() === 'PATCH') {
        return route.fulfill({ json: completedBooking });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Voltooien' }).first().click();

    await expect(page.getByText('Voltooid')).toBeVisible();
  });

  test('cancelling a booking changes status badge to Geannuleerd', async ({ page }) => {
    const cancelledBooking = {
      ...mockBooking,
      cancelledAtUtc: '2026-03-09T15:00:00Z',
    };

    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.route('**/api/bookings/booking-1/cancel', (route) => {
      if (route.request().method() === 'PATCH') {
        return route.fulfill({ json: cancelledBooking });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    await page.getByRole('button', { name: 'Annuleren' }).first().click();

    await expect(page.getByText('Geannuleerd')).toBeVisible();
  });

  test('edit dialog opens with pre-filled booking details', async ({ page }) => {
    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    // Click edit button on the booking row (selector to be refined once UI exists)
    await page.locator('button[title="Boeking bewerken"]').first().click();

    const dialog = page.locator('dialog[open]');
    await expect(dialog).toBeVisible();

    // Verify pre-filled values (selectors to be refined)
    await expect(dialog.getByText('Anna Bakker')).toBeVisible();
    await expect(dialog.getByText('Jan Jansen')).toBeVisible();
    await expect(dialog.getByText('Herenknippen')).toBeVisible();

    await page.keyboard.press('Escape');
  });

  test('filtering by date or staff updates the booking list', async ({ page }) => {
    await page.route('**/api/bookings', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ json: [mockBooking] });
      }
      return route.fulfill({ status: 404, body: '' });
    });

    await page.goto('/boekingen');

    // Filtering controls to be refined once the bookings UI is built
    await expect(page.getByText('Anna Bakker')).toBeVisible();
  });
});
