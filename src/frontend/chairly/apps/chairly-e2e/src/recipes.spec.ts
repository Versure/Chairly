import { expect, test } from './fixtures';

const CLIENT_ID = 'client-1';

const mockProfile = {
  id: CLIENT_ID,
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

const mockStats = {
  totalVisits: 2,
  lastVisitAtUtc: '2026-03-01T15:00:00Z',
  totalSpentAmount: 85,
  mostVisitedStaffMember: { id: 'staff-1', fullName: 'Jan Jansen', visitCount: 2 },
  mostBookedService: { id: 'svc-2', name: 'Kleuring', bookingCount: 1 },
  noShowCount: 0,
};

/**
 * Timeline with two completed bookings:
 *  - booking-1: has a recipe (Volledige kleuring) and booking notes
 *  - booking-2: completed but no recipe
 */
const mockTimeline = [
  {
    booking: {
      id: 'booking-1',
      startTime: '2026-02-15T10:00:00Z',
      endTime: '2026-02-15T11:00:00Z',
      durationMinutes: 60,
      status: 'Completed',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan Jansen',
      totalPrice: 60,
      notes: 'Warme tint gewenst',
      services: [
        {
          serviceId: 'svc-2',
          serviceName: 'Kleuring',
          durationMinutes: 60,
          price: 60,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: '2026-02-14T12:00:00Z',
      startedAtUtc: '2026-02-15T10:00:00Z',
      completedAtUtc: '2026-02-15T11:00:00Z',
      cancelledAtUtc: null,
      noShowAtUtc: null,
    },
    recipe: {
      id: 'recipe-1',
      bookingId: 'booking-1',
      bookingDate: '2026-02-15',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan Jansen',
      title: 'Volledige kleuring',
      notes: 'Warme tint toegepast',
      products: [{ name: 'Wella Illumina', brand: 'Wella', sortOrder: 0 }],
      createdAtUtc: '2026-02-15T11:00:00Z',
    },
    invoice: null,
  },
  {
    booking: {
      id: 'booking-2',
      startTime: '2026-03-01T14:00:00Z',
      endTime: '2026-03-01T14:30:00Z',
      durationMinutes: 30,
      status: 'Completed',
      staffMemberId: 'staff-1',
      staffMemberName: 'Jan Jansen',
      totalPrice: 25,
      notes: null,
      services: [
        {
          serviceId: 'svc-1',
          serviceName: 'Knippen',
          durationMinutes: 30,
          price: 25,
          sortOrder: 0,
        },
      ],
      confirmedAtUtc: '2026-02-28T12:00:00Z',
      startedAtUtc: '2026-03-01T14:00:00Z',
      completedAtUtc: '2026-03-01T15:00:00Z',
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

const mockRecipeFull = {
  id: 'recipe-1',
  bookingId: 'booking-1',
  clientId: CLIENT_ID,
  staffMemberId: 'staff-1',
  title: 'Volledige kleuring',
  notes: 'Warme tint toegepast',
  products: [
    {
      id: 'prod-1',
      name: 'Wella Illumina',
      brand: 'Wella',
      quantity: '60 ml',
      sortOrder: 0,
    },
  ],
  createdAtUtc: '2026-02-15T11:00:00Z',
  createdBy: 'staff-1',
  updatedAtUtc: null,
  updatedBy: null,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  // Timeline endpoint — default first-call payload
  await page.route(`**/api/clients/${CLIENT_ID}/timeline`, (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockTimelineResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Clients list (for navigation back)
  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockProfile] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // Recipe by booking (for "Recept bekijken / bewerken" click)
  await page.route('**/api/recipes/booking/booking-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockRecipeFull });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

// Test 1 (rewritten): timeline shows a card with "Recept bekijken / bewerken" link
// and the recipe title is visible on the card
test('timeline card with recipe shows Recept bekijken / bewerken button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await expect(page.getByText('Recept bekijken / bewerken')).toBeVisible();
  await expect(page.getByText('Volledige kleuring').first()).toBeVisible();
});

// Test 2 (deleted): "empty state when no recipes exist" — no longer applies.
// The empty timeline concept is tested in client-profile-timeline.spec.ts.

// Test 3 (deleted): "recipe products are displayed in the history card"
// Products are NOT displayed on the booking card in the new timeline UI.

// Test 4 (kept, retargeted): notes toggle on booking card
test('clicking Notities on a booking card expands the booking notes', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  // The toggle is on the booking with notes (booking-1 has notes: 'Warme tint gewenst')
  const notesToggle = page.getByRole('button', { name: 'Notities' });
  await expect(notesToggle).toBeVisible();
  await notesToggle.click();

  await expect(page.getByText('Warme tint gewenst')).toBeVisible();
});

// Test 5 (rewritten): clicking "Recept bekijken / bewerken" opens recipe form prefilled
test('clicking Recept bekijken / bewerken opens the recipe form dialog prefilled', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await page.getByText('Recept bekijken / bewerken').click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Titel behandeling')).toHaveValue('Volledige kleuring');

  await page.keyboard.press('Escape');
});

// Test 6 (rewritten): editing a recipe title and saving calls PUT and refreshes timeline
test('editing a recipe title and saving calls PUT and refreshes the timeline', async ({ page }) => {
  let putCalled = false;
  const updatedRecipe = {
    ...mockRecipeFull,
    title: 'Gedeeltelijke kleuring',
    updatedAtUtc: '2026-02-16T10:00:00Z',
    updatedBy: 'staff-1',
  };

  const updatedTimelineResponse = {
    ...mockTimelineResponse,
    timeline: [
      {
        ...mockTimeline[0],
        recipe: {
          ...mockTimeline[0].recipe,
          title: 'Gedeeltelijke kleuring',
          updatedAtUtc: '2026-02-16T10:00:00Z',
        },
      },
      mockTimeline[1],
    ],
  };

  await setupApiMocks(page);

  // Mock PUT /api/recipes/recipe-1
  await page.route('**/api/recipes/recipe-1', (route) => {
    if (route.request().method() === 'PUT') {
      putCalled = true;
      return route.fulfill({ json: updatedRecipe });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // After save, the page reloads the timeline — return updated data on the second call
  let timelineCallCount = 0;
  await page.route(`**/api/clients/${CLIENT_ID}/timeline`, (route) => {
    timelineCallCount++;
    if (route.request().method() === 'GET') {
      if (timelineCallCount > 1) {
        return route.fulfill({ json: updatedTimelineResponse });
      }
      return route.fulfill({ json: mockTimelineResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto(`/klanten/${CLIENT_ID}`);
  await expect(page.getByText('Volledige kleuring').first()).toBeVisible();

  await page.getByText('Recept bekijken / bewerken').click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Titel behandeling').fill('Gedeeltelijke kleuring');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Gedeeltelijke kleuring')).toBeVisible();
  expect(putCalled).toBe(true);
});

// Test 7 (kept, retargeted): completed booking without recipe shows "Recept toevoegen" button
test('completed booking without recipe shows Recept toevoegen button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await expect(page.getByRole('button', { name: 'Recept toevoegen' })).toBeVisible();
});

// Test 8 (rewritten): clicking "Recept toevoegen" opens form, saves, and the new title appears
test('clicking Recept toevoegen opens recipe form, saves, and new title appears in timeline', async ({
  page,
}) => {
  const newRecipe = {
    id: 'recipe-2',
    bookingId: 'booking-2',
    clientId: CLIENT_ID,
    staffMemberId: 'staff-1',
    title: 'Knippen standaard',
    notes: 'Kort model',
    products: [
      {
        id: 'prod-2',
        name: 'Styling gel',
        brand: 'Redken',
        quantity: '20 ml',
        sortOrder: 0,
      },
    ],
    createdAtUtc: '2026-03-01T16:00:00Z',
    createdBy: 'staff-1',
    updatedAtUtc: null,
    updatedBy: null,
  };

  const updatedTimelineResponse = {
    ...mockTimelineResponse,
    timeline: [
      mockTimeline[0],
      {
        ...mockTimeline[1],
        recipe: {
          id: 'recipe-2',
          bookingId: 'booking-2',
          bookingDate: '2026-03-01',
          staffMemberId: 'staff-1',
          staffMemberName: 'Jan Jansen',
          title: 'Knippen standaard',
          notes: 'Kort model',
          products: [{ name: 'Styling gel', brand: 'Redken', sortOrder: 0 }],
          createdAtUtc: '2026-03-01T16:00:00Z',
        },
      },
    ],
  };

  let postCalled = false;
  await setupApiMocks(page);

  // Mock POST /api/recipes
  await page.route('**/api/recipes', (route) => {
    if (route.request().method() === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newRecipe, status: 201 });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // After save, the page reloads the timeline — return updated data on the second call
  let timelineCallCount = 0;
  await page.route(`**/api/clients/${CLIENT_ID}/timeline`, (route) => {
    timelineCallCount++;
    if (route.request().method() === 'GET') {
      if (timelineCallCount > 1) {
        return route.fulfill({ json: updatedTimelineResponse });
      }
      return route.fulfill({ json: mockTimelineResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto(`/klanten/${CLIENT_ID}`);

  const addButton = page.getByRole('button', { name: 'Recept toevoegen' });
  await expect(addButton).toBeVisible();
  await addButton.click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();

  // Fill in the form
  await dialog.getByLabel('Titel behandeling').fill('Knippen standaard');
  await dialog.getByLabel('Notities').fill('Kort model');

  // Add a product
  await dialog.getByRole('button', { name: /Product toevoegen/ }).click();
  await dialog.getByLabel('Naam').fill('Styling gel');
  await dialog.getByLabel('Merk').fill('Redken');
  await dialog.getByLabel('Hoeveelheid').fill('20 ml');

  // Save and verify the result
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Knippen standaard')).toBeVisible();
  expect(postCalled).toBe(true);
});

// Test 9 (kept as-is): "Terug naar klanten" link
test('Terug naar klanten link navigates back to the clients list', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto(`/klanten/${CLIENT_ID}`);

  await page.getByRole('link', { name: /Terug naar klanten/ }).click();
  await expect(page).toHaveURL(/\/klanten$/);
});
