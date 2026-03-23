import { expect, test } from './fixtures';

const mockDashboardResponse = {
  todaysBookingsCount: 3,
  todaysBookings: [
    {
      id: 'b1',
      clientId: 'c1',
      clientName: 'Jan Jansen',
      staffMemberId: 's1',
      staffMemberName: 'Piet Bakker',
      startTime: '2026-03-23T09:00:00Z',
      endTime: '2026-03-23T09:30:00Z',
      status: 'Scheduled',
      serviceNames: ['Herenknippen'],
    },
    {
      id: 'b2',
      clientId: 'c2',
      clientName: 'Karin de Vries',
      staffMemberId: 's1',
      staffMemberName: 'Piet Bakker',
      startTime: '2026-03-23T10:00:00Z',
      endTime: '2026-03-23T10:45:00Z',
      status: 'Confirmed',
      serviceNames: ['Damesknippen', 'Föhnen'],
    },
    {
      id: 'b3',
      clientId: 'c3',
      clientName: 'Bram Willems',
      staffMemberId: 's2',
      staffMemberName: 'Lisa Smit',
      startTime: '2026-03-23T11:00:00Z',
      endTime: '2026-03-23T11:30:00Z',
      status: 'InProgress',
      serviceNames: ['Herenknippen'],
    },
  ],
  upcomingBookings: [
    {
      id: 'b4',
      clientId: 'c1',
      clientName: 'Jan Jansen',
      staffMemberId: 's1',
      staffMemberName: 'Piet Bakker',
      startTime: '2026-03-24T09:00:00Z',
      endTime: '2026-03-24T09:30:00Z',
      status: 'Scheduled',
      serviceNames: ['Herenknippen'],
    },
  ],
  newClientsThisWeek: 5,
  revenueThisWeek: 450.0,
  revenueThisMonth: 1850.0,
};

const emptyDashboardResponse = {
  todaysBookingsCount: 0,
  todaysBookings: [],
  upcomingBookings: [],
  newClientsThisWeek: 0,
  revenueThisWeek: 0,
  revenueThisMonth: 0,
};

async function setupDashboardMock(
  page: import('@playwright/test').Page,
  response: typeof mockDashboardResponse | typeof emptyDashboardResponse = mockDashboardResponse,
): Promise<void> {
  await page.route('**/api/dashboard', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: response });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test.describe('Dashboard', () => {
  test('page loads and shows Dashboard heading', async ({ page }) => {
    await setupDashboardMock(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('redirects from / to /dashboard', async ({ page }) => {
    await setupDashboardMock(page);
    await page.goto('/');
    await page.waitForURL('**/dashboard');
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  });

  test('shows "Boekingen vandaag" stat card', async ({ page }) => {
    await setupDashboardMock(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('paragraph').filter({ hasText: 'Boekingen vandaag' }).first()).toBeVisible();
  });

  test('shows "Boekingen vandaag" section heading', async ({ page }) => {
    await setupDashboardMock(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Boekingen vandaag' })).toBeVisible();
  });

  test('shows "Aankomende boekingen" section heading', async ({ page }) => {
    await setupDashboardMock(page);
    await page.goto('/dashboard');
    await expect(page.getByRole('heading', { name: 'Aankomende boekingen' })).toBeVisible();
  });

  test('shows empty messages when no bookings exist', async ({ page }) => {
    await setupDashboardMock(page, emptyDashboardResponse);
    await page.goto('/dashboard');
    await expect(page.getByText('Geen boekingen vandaag')).toBeVisible();
    await expect(page.getByText('Geen aankomende boekingen')).toBeVisible();
  });
});
