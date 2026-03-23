import { expect, test } from './fixtures';

const subscriptionId = '00000000-0000-0000-0000-000000000001';

const mockPendingDetail = {
  id: subscriptionId,
  salonName: 'Salon De Schaar',
  ownerFirstName: 'Jan',
  ownerLastName: 'Jansen',
  email: 'jan@deschaar.nl',
  phoneNumber: '+31612345678',
  plan: 'starter',
  billingCycle: null,
  isTrial: true,
  status: 'trial',
  trialEndsAtUtc: '2026-04-15T10:00:00Z',
  createdAtUtc: '2026-01-15T10:00:00Z',
  createdByName: null,
  provisionedAtUtc: null,
  provisionedByName: null,
  cancelledAtUtc: null,
  cancelledByName: null,
  cancellationReason: null,
};

const mockProvisionedDetail = {
  ...mockPendingDetail,
  status: 'provisioned',
  isTrial: false,
  trialEndsAtUtc: null,
  billingCycle: 'Monthly',
  provisionedAtUtc: '2026-01-16T10:00:00Z',
  provisionedByName: 'admin-user-id',
};

const mockCancelledDetail = {
  ...mockProvisionedDetail,
  status: 'cancelled',
  cancelledAtUtc: '2026-02-01T10:00:00Z',
  cancelledByName: 'admin-user-id',
  cancellationReason: 'Niet betaald',
};

test.describe('Subscription Detail - Trial', () => {
  test.beforeEach(async ({ page }) => {
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockPendingDetail),
      }),
    );
    // Mock list endpoint for any background refreshes
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
      }),
    );
  });

  test('should display subscription detail', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    // salonName in the page h1 (the second h1, after "Chairly Admin" in sidebar)
    await expect(page.locator('main h1')).toHaveText('Salon De Schaar');
    await expect(page.getByText('Jan Jansen')).toBeVisible();
    await expect(page.getByText('jan@deschaar.nl')).toBeVisible();
    await expect(page.getByText('+31612345678')).toBeVisible();
  });

  test('should show trial status badge', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    // The header badge is a span adjacent to the h1 in main
    await expect(page.locator('main h1 ~ span')).toContainText('Proefperiode');
  });

  test('should show back link', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(page.getByText('Terug naar overzicht')).toBeVisible();
  });

  test('should show Activeren and Annuleren buttons for trial subscription', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    // Scope to the Acties section using visible: true filter to avoid closed dialogs
    await expect(
      page.locator('button', { hasText: 'Activeren' }).filter({ visible: true }),
    ).toBeVisible();
    await expect(
      page.locator('button', { hasText: 'Annuleren' }).filter({ visible: true }).first(),
    ).toBeVisible();
  });

  test('should open provision dialog when clicking Activeren', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await page.locator('button', { hasText: 'Activeren' }).filter({ visible: true }).click();
    await expect(page.getByText('Abonnement activeren')).toBeVisible();
    await expect(
      page.locator('dialog[open]').getByText('Salon De Schaar', { exact: false }),
    ).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('should open cancel dialog when clicking Annuleren', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await page
      .locator('button', { hasText: 'Annuleren' })
      .filter({ visible: true })
      .first()
      .click();
    await expect(page.locator('dialog[open] h2')).toContainText('Abonnement annuleren');
    await page.keyboard.press('Escape');
  });
});

test.describe('Subscription Detail - Provisioned', () => {
  test.beforeEach(async ({ page }) => {
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockProvisionedDetail),
      }),
    );
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
      }),
    );
  });

  test('should show Plan wijzigen and Annuleren buttons for provisioned subscription', async ({
    page,
  }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(
      page.locator('button', { hasText: 'Plan wijzigen' }).filter({ visible: true }),
    ).toBeVisible();
    await expect(
      page.locator('button', { hasText: 'Annuleren' }).filter({ visible: true }).first(),
    ).toBeVisible();
  });

  test('should open update plan dialog when clicking Plan wijzigen', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await page.locator('button', { hasText: 'Plan wijzigen' }).filter({ visible: true }).click();
    await expect(page.locator('dialog[open] h2')).toContainText('Plan wijzigen');
    await page.keyboard.press('Escape');
  });

  test('should show timeline with provisioned event', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(page.getByText('Geactiveerd op', { exact: false })).toBeVisible();
  });
});

test.describe('Subscription Detail - Cancelled', () => {
  test.beforeEach(async ({ page }) => {
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCancelledDetail),
      }),
    );
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
      }),
    );
  });

  test('should show no action buttons for cancelled subscription', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(page.getByText('Dit abonnement is geannuleerd.')).toBeVisible();
    // For cancelled state: verify the action buttons are not present in main content
    await expect(
      page.locator('button', { hasText: 'Activeren' }).filter({ visible: true }),
    ).toHaveCount(0);
    await expect(
      page.locator('button', { hasText: 'Plan wijzigen' }).filter({ visible: true }),
    ).toHaveCount(0);
  });

  test('should show cancellation reason in timeline', async ({ page }) => {
    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(page.getByText('Niet betaald', { exact: false })).toBeVisible();
  });
});

test.describe('Subscription Detail - Confirm Flows', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 25 }),
      }),
    );
  });

  test('should provision subscription and show updated status', async ({ page }) => {
    // Mock the detail endpoint with trial subscription
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockPendingDetail),
        });
      }
      return route.continue();
    });

    // Mock the provision endpoint
    const provisionedResponse = {
      ...mockPendingDetail,
      status: 'provisioned',
      isTrial: false,
      trialEndsAtUtc: null,
      provisionedAtUtc: '2026-03-22T10:00:00Z',
      provisionedByName: 'admin',
    };
    await page.route(`**/api/admin/subscriptions/${subscriptionId}/provision`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(provisionedResponse),
      }),
    );

    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(page.locator('main h1 ~ span')).toContainText('Proefperiode');

    await page.locator('button', { hasText: 'Activeren' }).filter({ visible: true }).click();
    await expect(page.getByText('Abonnement activeren')).toBeVisible();

    // Click the confirm button in the dialog
    const confirmButton = page.locator('dialog[open] button', { hasText: 'Activeren' });
    await confirmButton.click();
  });

  test('should cancel subscription with reason and show updated status', async ({ page }) => {
    // Mock the detail endpoint with provisioned subscription
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockProvisionedDetail),
        });
      }
      return route.continue();
    });

    // Mock the cancel endpoint
    const cancelledResponse = {
      ...mockProvisionedDetail,
      status: 'cancelled',
      cancelledAtUtc: '2026-03-22T10:00:00Z',
      cancelledByName: 'admin',
      cancellationReason: 'Klant wil opzeggen',
    };
    await page.route(`**/api/admin/subscriptions/${subscriptionId}/cancel`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(cancelledResponse),
      }),
    );

    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(
      page.locator('button', { hasText: 'Annuleren' }).filter({ visible: true }).first(),
    ).toBeVisible();

    await page
      .locator('button', { hasText: 'Annuleren' })
      .filter({ visible: true })
      .first()
      .click();
    await expect(page.locator('dialog[open] h2')).toContainText('Abonnement annuleren');

    // Fill in the reason
    await page.locator('dialog[open] textarea').fill('Klant wil opzeggen');

    // Click the confirm button in the dialog (the second "Annuleren" button in dialog)
    const confirmButton = page.locator('dialog[open] button').last();
    await confirmButton.click();
  });

  test('should update subscription plan and show updated details', async ({ page }) => {
    // Mock the detail endpoint with provisioned subscription
    await page.route(`**/api/admin/subscriptions/${subscriptionId}`, (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockProvisionedDetail),
        });
      }
      return route.continue();
    });

    // Mock the plan update endpoint
    const updatedResponse = {
      ...mockProvisionedDetail,
      plan: 'salon',
      billingCycle: 'Annual',
    };
    await page.route(`**/api/admin/subscriptions/${subscriptionId}/plan`, (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(updatedResponse),
      }),
    );

    await page.goto(`/abonnementen/${subscriptionId}`);
    await expect(
      page.locator('button', { hasText: 'Plan wijzigen' }).filter({ visible: true }),
    ).toBeVisible();

    await page.locator('button', { hasText: 'Plan wijzigen' }).filter({ visible: true }).click();
    await expect(page.locator('dialog[open] h2')).toContainText('Plan wijzigen');

    // Change selections in dialog
    await page.locator('dialog[open] select#plan-select').selectOption('salon');
    await page.locator('dialog[open] select#billing-cycle-select').selectOption('Annual');

    // Click the confirm button (Opslaan)
    const confirmButton = page.locator('dialog[open] button', { hasText: 'Opslaan' });
    await confirmButton.click();
  });
});
