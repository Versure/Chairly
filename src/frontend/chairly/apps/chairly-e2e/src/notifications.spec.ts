import { expect, test } from './fixtures';

const mockNotifications = [
  {
    id: 'notif-1',
    type: 'BookingConfirmation',
    recipientName: 'Jan de Vries',
    channel: 'Email',
    status: 'Verzonden',
    scheduledAtUtc: '2026-03-10T10:00:00Z',
    sentAtUtc: '2026-03-10T10:00:05Z',
    retryCount: 0,
    referenceId: 'booking-1',
  },
  {
    id: 'notif-2',
    type: 'BookingReminder',
    recipientName: 'Petra Jansen',
    channel: 'Email',
    status: 'Wachtend',
    scheduledAtUtc: '2026-03-11T09:00:00Z',
    retryCount: 0,
    referenceId: 'booking-2',
  },
  {
    id: 'notif-3',
    type: 'BookingCancellation',
    recipientName: 'Kees Bakker',
    channel: 'Email',
    status: 'Mislukt',
    scheduledAtUtc: '2026-03-09T14:00:00Z',
    failedAtUtc: '2026-03-09T14:01:00Z',
    failureReason: 'SMTP-verbinding mislukt',
    retryCount: 3,
    referenceId: 'booking-3',
  },
];

async function setupApiMocks(
  page: import('@playwright/test').Page,
  notifications: typeof mockNotifications = mockNotifications,
): Promise<void> {
  await page.route('**/api/notifications', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: notifications });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigates to /meldingen and shows the Meldingen heading', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/meldingen');

  await expect(page.getByRole('heading', { name: 'Meldingen', level: 1 })).toBeVisible();
});

test('shows empty state when no notifications exist', async ({ page }) => {
  await setupApiMocks(page, []);
  await page.goto('/meldingen');

  await expect(page.getByText('Nog geen meldingen verstuurd')).toBeVisible();
});

test('displays notification table with correct Dutch type labels', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/meldingen');

  await expect(page.getByRole('table')).toBeVisible();

  // Verify Dutch type labels
  await expect(page.getByText('Bevestiging')).toBeVisible();
  await expect(page.getByText('Herinnering')).toBeVisible();
  await expect(page.getByText('Annulering')).toBeVisible();
});

test('displays recipient names in the table', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/meldingen');

  await expect(page.getByText('Jan de Vries')).toBeVisible();
  await expect(page.getByText('Petra Jansen')).toBeVisible();
  await expect(page.getByText('Kees Bakker')).toBeVisible();
});

test('shows correct status badges with appropriate styling', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/meldingen');

  // All three statuses should be visible as badge text (exact match to avoid matching column headers like "Verzonden op")
  await expect(page.getByText('Verzonden', { exact: true })).toBeVisible();
  await expect(page.getByText('Wachtend', { exact: true })).toBeVisible();
  await expect(page.getByText('Mislukt', { exact: true })).toBeVisible();
});

test('mislukt badge shows failure reason as tooltip', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/meldingen');

  const misluktBadge = page.locator('span', { hasText: 'Mislukt' });
  await expect(misluktBadge).toHaveAttribute('title', 'SMTP-verbinding mislukt');
});

test('shows dash when sentAtUtc is not set', async ({ page }) => {
  await setupApiMocks(page, [
    {
      id: 'notif-pending',
      type: 'BookingReminder',
      recipientName: 'Test Klant',
      channel: 'Email',
      status: 'Wachtend',
      scheduledAtUtc: '2026-03-12T10:00:00Z',
      retryCount: 0,
      referenceId: 'booking-pending',
    },
  ]);
  await page.goto('/meldingen');

  // The "Verzonden op" column should show a dash for pending notifications
  const row = page.getByRole('row').filter({ hasText: 'Test Klant' });
  const cells = row.getByRole('cell');
  // The last cell (Verzonden op) should contain a dash
  await expect(cells.last()).toContainText('\u2014');
});
