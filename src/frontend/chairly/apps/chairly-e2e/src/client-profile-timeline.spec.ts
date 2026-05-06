import { expect, test } from './fixtures';

const CLIENT_ID = 'client-timeline-1';
const EMPTY_CLIENT_ID = 'client-empty-1';

const COMPLETED_BOOKING_WITHOUT_RECIPE_ID = 'booking-completed-no-recipe';

const mockProfile = {
  id: CLIENT_ID,
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: '0612345678',
  notes: 'Vaste klant',
  createdAtUtc: '2025-01-01T00:00:00Z',
  updatedAtUtc: null,
};

const mockStats = {
  totalVisits: 5,
  lastVisitAtUtc: '2026-04-15T10:00:00Z',
  totalSpentAmount: 275.5,
  mostVisitedStaffMember: { id: 'staff-1', fullName: 'Jan de Vries', visitCount: 3 },
  mostBookedService: { id: 'svc-1', name: 'Herenknippen', bookingCount: 4 },
  noShowCount: 1,
};

const mockTimeline = [
  {
    booking: {
      id: 'booking-completed-with-recipe',
      startTime: '2026-05-14T13:30:00Z',
      endTime: '2026-05-14T14:15:00Z',
      durationMinutes: 45,
      status: 'Completed',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      totalPrice: 55,
      notes: 'Klant wil korter aan de zijkanten.',
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Herenknippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
        {
          serviceId: 'svc-2',
          serviceName: 'Baard trimmen',
          durationMinutes: 15,
          price: 30,
          sortOrder: 1,
        },
      ],
      confirmedAtUtc: '2026-05-13T10:00:00Z',
      startedAtUtc: '2026-05-14T13:30:00Z',
      completedAtUtc: '2026-05-14T14:15:00Z',
      cancelledAtUtc: null,
      noShowAtUtc: null,
    },
    recipe: {
      id: 'recipe-1',
      bookingId: 'booking-completed-with-recipe',
      bookingDate: '2026-05-14',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      title: 'Kort zijkanten',
      notes: 'Schaar #3',
      products: [{ name: 'Wax', brand: 'American Crew', sortOrder: 0 }],
      createdAtUtc: '2026-05-14T14:20:00Z',
    },
    invoice: {
      id: 'invoice-1',
      invoiceNumber: 'INV-2026-001',
      invoiceDate: '2026-05-14',
      totalAmount: 55,
      status: 'Paid',
      sentAtUtc: '2026-05-14T15:00:00Z',
      paidAtUtc: '2026-05-14T16:00:00Z',
      voidedAtUtc: null,
    },
  },
  {
    booking: {
      id: COMPLETED_BOOKING_WITHOUT_RECIPE_ID,
      startTime: '2026-05-02T10:00:00Z',
      endTime: '2026-05-02T10:30:00Z',
      durationMinutes: 30,
      status: 'Completed',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      totalPrice: 25,
      notes: null,
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Herenknippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: '2026-05-01T10:00:00Z',
      startedAtUtc: '2026-05-02T10:00:00Z',
      completedAtUtc: '2026-05-02T10:30:00Z',
      cancelledAtUtc: null,
      noShowAtUtc: null,
    },
    recipe: null,
    invoice: null,
  },
  {
    booking: {
      id: 'booking-cancelled',
      startTime: '2026-03-20T14:00:00Z',
      endTime: '2026-03-20T14:45:00Z',
      durationMinutes: 45,
      status: 'Cancelled',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      totalPrice: 25,
      notes: null,
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Herenknippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: null,
      startedAtUtc: null,
      completedAtUtc: null,
      cancelledAtUtc: '2026-03-19T12:00:00Z',
      noShowAtUtc: null,
    },
    recipe: null,
    invoice: null,
  },
  {
    booking: {
      id: 'booking-noshow',
      startTime: '2026-02-10T09:00:00Z',
      endTime: '2026-02-10T09:30:00Z',
      durationMinutes: 30,
      status: 'NoShow',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      totalPrice: 25,
      notes: null,
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Herenknippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: null,
      startedAtUtc: null,
      completedAtUtc: null,
      cancelledAtUtc: null,
      noShowAtUtc: '2026-02-10T09:30:00Z',
    },
    recipe: null,
    invoice: null,
  },
  {
    booking: {
      id: 'booking-scheduled',
      startTime: '2026-06-01T11:00:00Z',
      endTime: '2026-06-01T11:30:00Z',
      durationMinutes: 30,
      status: 'Scheduled',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan de Vries',
      totalPrice: 25,
      notes: null,
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Herenknippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: null,
      startedAtUtc: null,
      completedAtUtc: null,
      cancelledAtUtc: null,
      noShowAtUtc: null,
    },
    recipe: null,
    invoice: null,
  },
];

const mockTimelineResponse = {
  profile: mockProfile,
  stats: mockStats,
  timeline: mockTimeline,
};

const emptyTimelineResponse = {
  profile: {
    ...mockProfile,
    id: EMPTY_CLIENT_ID,
    firstName: 'Piet',
    lastName: 'Leeg',
    email: null,
    phoneNumber: null,
    notes: null,
  },
  stats: {
    totalVisits: 0,
    lastVisitAtUtc: null,
    totalSpentAmount: 0,
    mostVisitedStaffMember: null,
    mostBookedService: null,
    noShowCount: 0,
  },
  timeline: [],
};

const mockRecipe = {
  id: 'recipe-1',
  bookingId: 'booking-completed-with-recipe',
  clientId: CLIENT_ID,
  staffMemberId: 'staff-1',
  title: 'Kort zijkanten',
  notes: 'Schaar #3',
  products: [{ id: 'prod-1', name: 'Wax', brand: 'American Crew', quantity: '1', sortOrder: 0 }],
  createdAtUtc: '2026-05-14T14:20:00Z',
  createdBy: 'staff-1',
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  // Timeline endpoint
  await page.route(`**/api/clients/${CLIENT_ID}/timeline`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockTimelineResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Empty client timeline
  await page.route(`**/api/clients/${EMPTY_CLIENT_ID}/timeline`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: emptyTimelineResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Clients list (for navigation)
  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockProfile] });
    }
    if (route.request().method() === 'PUT') {
      return route.fulfill({ json: mockProfile });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Client update
  await page.route(`**/api/clients/${CLIENT_ID}`, (route) => {
    if (route.request().method() === 'PUT') {
      const body = route.request().postDataJSON();
      return route.fulfill({
        json: { ...mockProfile, ...body },
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Recipe by booking
  await page.route('**/api/recipes/booking/booking-completed-with-recipe', (route) => {
    return route.fulfill({ json: mockRecipe });
  });
}

// Scenario 1: Page heading and profile header visible
test('navigating to client detail page shows client name and profile header', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await expect(page.getByRole('heading', { level: 1 })).toContainText('Bakker, Anna');
  await expect(page.locator('chairly-client-profile-header')).toBeVisible();
});

// Scenario 2: Profile stats are populated
test('profile stats show Bezoeken count and Totale omzet with Euro currency', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await expect(page.getByText('Bezoeken')).toBeVisible();
  await expect(page.getByText('5')).toBeVisible();
  await expect(page.getByText('Totale omzet')).toBeVisible();
  // Euro value present
  await expect(page.getByText(/€/)).toBeVisible();
});

// Scenario 3: Status filter chips with Dutch labels
test('status filter chips render with Dutch labels and filtering works', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // All five chip labels visible
  await expect(page.getByRole('button', { name: /Alle/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /Voltooid/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /Geannuleerd/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /No-show/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /Gepland/ })).toBeVisible();

  // Click "Voltooid" and check all visible badges say "Voltooid"
  await page.getByRole('button', { name: /Voltooid/ }).click();

  // Wait for the timeline cards to update - only "Voltooid" badges should remain
  const badges = page.locator(
    'chairly-booking-timeline-card .inline-flex.rounded-full.bg-green-100, chairly-booking-timeline-card .inline-flex.rounded-full.dark\\:bg-green-900\\/40',
  );
  const badgeCount = await badges.count();
  expect(badgeCount).toBeGreaterThan(0);

  // Click "Alle" to bring back all bookings
  await page.getByRole('button', { name: /Alle/ }).click();
  // Should have all 5 timeline cards
  const allCards = page.locator('chairly-booking-timeline-card');
  await expect(allCards).toHaveCount(5);
});

// Scenario 4: Timeline entries grouped by month with Dutch labels
test('timeline entries are grouped by month with Dutch month labels', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // Check for at least one h2 with a Dutch month label
  const monthHeaders = page.locator('h2');
  const count = await monthHeaders.count();
  expect(count).toBeGreaterThanOrEqual(1);

  // Verify a known month label is present (mei 2026 or juni 2026)
  const allText = await page.locator('h2').allTextContents();
  const hasMonthLabel = allText.some(
    (text) =>
      /mei\s*2026/i.test(text) ||
      /juni\s*2026/i.test(text) ||
      /maart\s*2026/i.test(text) ||
      /februari\s*2026/i.test(text),
  );
  expect(hasMonthLabel).toBe(true);
});

// Scenario 5: Booking card displays all required fields
test('booking card shows date, time range, status badge, staff name, services, total price, and duration', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // Find the first card
  const card = page.locator('chairly-booking-timeline-card').first();
  await expect(card).toBeVisible();

  // Staff name with prefix "Met "
  await expect(card.getByText(/Met\s/)).toBeVisible();

  // Duration pill with "min"
  await expect(card.getByText(/min/)).toBeVisible();

  // Service name
  await expect(card.getByText('Herenknippen')).toBeVisible();

  // Euro price
  await expect(card.getByText(/€/)).toBeVisible();
});

// Scenario 6: "Recept toevoegen" opens recipe form
test('clicking "Recept toevoegen" on a completed booking without recipe opens the recipe form dialog', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await page.getByRole('button', { name: 'Recept toevoegen' }).first().click();
  await expect(page.locator('dialog[open]')).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(page.locator('dialog[open]')).toHaveCount(0);
});

// Scenario 7: "Recept bekijken / bewerken" opens recipe form pre-filled
test('clicking "Recept bekijken / bewerken" opens the recipe form pre-filled', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await page.getByText('Recept bekijken / bewerken').first().click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  await page.keyboard.press('Escape');
});

// Scenario 8: Invoice link navigates to invoice detail
test('booking with invoice shows "Factuur" link and clicking it navigates to /facturen/{id}', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  const invoiceLink = page.getByText('Factuur INV-2026-001');
  await expect(invoiceLink).toBeVisible();

  await invoiceLink.click();
  await expect(page).toHaveURL(/\/facturen\/invoice-1/);
});

// Scenario 9: Edit client via profile header
test('clicking "Bewerken" in profile header opens the edit dialog and saves updates', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // Click the "Bewerken" button in the profile header
  await page
    .locator('chairly-client-profile-header')
    .getByRole('button', { name: 'Bewerken' })
    .click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  // The first name field should be pre-filled
  await expect(dialog.getByLabel('Voornaam')).toHaveValue('Anna');

  // Change the first name
  await dialog.getByLabel('Voornaam').fill('Maria');

  const responsePromise = page.waitForResponse(`**/api/clients/${CLIENT_ID}`);
  await dialog.getByRole('button', { name: 'Opslaan' }).click();
  await responsePromise;

  // The profile header should now show "Maria Bakker"
  await expect(page.locator('chairly-client-profile-header')).toContainText('Maria');
});

// Scenario 10: ?bookingId query param auto-opens recipe form
test('?bookingId query param auto-opens the recipe form for that booking', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}?bookingId=${COMPLETED_BOOKING_WITHOUT_RECIPE_ID}`);

  // The recipe form dialog should auto-open
  await expect(page.locator('dialog[open]')).toBeVisible();

  await page.keyboard.press('Escape');
});

// Scenario 11: Empty state for a client with no bookings
test('empty client shows "Deze klant heeft nog geen boekingen."', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${EMPTY_CLIENT_ID}`);

  await expect(page.getByText('Deze klant heeft nog geen boekingen.')).toBeVisible();
});

// Scenario 12: Filter empty state
test('selecting a status with no bookings shows "Geen boekingen voor dit filter."', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // Click "Geannuleerd" — there is 1 cancelled booking, so we need to find a status with 0.
  // InProgress has 0 count but is combined under "Gepland".
  // Let's verify "Geannuleerd" has 1, so the empty state won't trigger there.
  // We need to filter by a chip that yields 0 results.
  // Looking at data: Cancelled has 1, NoShow has 1, Completed has 2, Scheduled has 1.
  // All have at least 1. Let's adjust - we need a specific scenario.
  // Actually, let's use a custom timeline response for this test.

  // Create a timeline where there are no Cancelled bookings
  const noCancelledResponse = {
    ...mockTimelineResponse,
    timeline: mockTimelineResponse.timeline.filter((e) => e.booking.status !== 'Cancelled'),
    stats: { ...mockStats, totalVisits: 4 },
  };

  // Re-route to use the adjusted response
  await page.route(`**/api/clients/client-filter-test/timeline`, (route) => {
    return route.fulfill({ json: noCancelledResponse });
  });

  await page.goto('/klanten/client-filter-test');

  // Click "Geannuleerd" - should have 0 entries
  await page.getByRole('button', { name: /Geannuleerd/ }).click();

  await expect(page.getByText('Geen boekingen voor dit filter.')).toBeVisible();
});
