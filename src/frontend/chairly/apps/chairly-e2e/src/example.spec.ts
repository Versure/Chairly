import { expect, test } from './fixtures';

test('has title', async ({ page }) => {
  await page.route('**/api/dashboard', (route) => route.fulfill({ json: { todaysBookingsCount: 0, todaysBookings: [], upcomingBookings: [], newClientsThisWeek: 0, revenueThisWeek: 0, revenueThisMonth: 0 } }));
  await page.goto('/');

  // App redirects to /dashboard — expect h1 to contain 'Dashboard'.
  await expect(page.locator('h1')).toContainText('Dashboard');
});
