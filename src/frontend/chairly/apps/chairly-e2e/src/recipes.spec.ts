import { expect, test } from './fixtures';

const mockClient = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

const mockCompletedBooking = {
  id: 'booking-2',
  clientId: 'client-1',
  staffMemberId: 'staff-1',
  startTime: '2026-03-01T14:00:00Z',
  endTime: '2026-03-01T15:00:00Z',
  notes: null,
  status: 'Completed',
  services: [
    { serviceId: 'svc-1', serviceName: 'Knippen', duration: '00:30:00', price: 25, sortOrder: 0 },
  ],
  createdAtUtc: '2026-02-28T10:00:00Z',
  updatedAtUtc: null,
  confirmedAtUtc: '2026-02-28T12:00:00Z',
  startedAtUtc: '2026-03-01T14:00:00Z',
  completedAtUtc: '2026-03-01T15:00:00Z',
  cancelledAtUtc: null,
  noShowAtUtc: null,
};

const mockBookingWithRecipe = {
  id: 'booking-1',
  clientId: 'client-1',
  staffMemberId: 'staff-1',
  startTime: '2026-02-15T10:00:00Z',
  endTime: '2026-02-15T11:00:00Z',
  notes: null,
  status: 'Completed',
  services: [
    { serviceId: 'svc-2', serviceName: 'Kleuring', duration: '01:00:00', price: 60, sortOrder: 0 },
  ],
  createdAtUtc: '2026-02-14T10:00:00Z',
  updatedAtUtc: null,
  confirmedAtUtc: '2026-02-14T12:00:00Z',
  startedAtUtc: '2026-02-15T10:00:00Z',
  completedAtUtc: '2026-02-15T11:00:00Z',
  cancelledAtUtc: null,
  noShowAtUtc: null,
};

const mockRecipeSummary = {
  id: 'recipe-1',
  bookingId: 'booking-1',
  bookingDate: '2026-02-15T10:00:00Z',
  staffMemberId: 'staff-1',
  staffMemberName: 'Jan Jansen',
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
  updatedAtUtc: null,
};

const mockRecipeFull = {
  id: 'recipe-1',
  bookingId: 'booking-1',
  clientId: 'client-1',
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

interface SetupOptions {
  recipes?: (typeof mockRecipeSummary)[];
  bookings?: (typeof mockCompletedBooking)[];
}

async function setupApiMocks(
  page: import('@playwright/test').Page,
  options?: SetupOptions,
): Promise<void> {
  const recipes = options?.recipes ?? [mockRecipeSummary];
  const bookings = options?.bookings ?? [mockBookingWithRecipe, mockCompletedBooking];

  await page.route('**/api/clients', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockClient] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/bookings', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: bookings });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/clients/client-1/recipes', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: recipes });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/recipes/booking/booking-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockRecipeFull });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('client detail page shows Behandelgeschiedenis heading with recipe list', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  await expect(page.getByRole('heading', { name: 'Behandelgeschiedenis' })).toBeVisible();
  await expect(page.getByText('Volledige kleuring')).toBeVisible();
  await expect(page.getByText('Jan Jansen')).toBeVisible();
});

test('client detail page shows empty state when no recipes exist', async ({ page }) => {
  await setupApiMocks(page, { recipes: [] });
  await page.goto('/klanten/client-1');

  await expect(page.getByText('Nog geen behandelrecords voor deze klant')).toBeVisible();
});

test('recipe products are displayed in the history card', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  await expect(page.getByText('Wella Illumina')).toBeVisible();
  await expect(page.getByText('Wella')).toBeVisible();
  await expect(page.getByText('60 ml')).toBeVisible();
});

test('clicking Notities tonen expands the notes section', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  const notesToggle = page.getByRole('button', { name: /Notities tonen/ });
  await expect(notesToggle).toBeVisible();
  await notesToggle.click();

  await expect(page.getByText('Warme tint toegepast')).toBeVisible();
});

test('clicking Recept bewerken on a recipe opens the recipe form dialog prefilled', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  await page.getByRole('button', { name: 'Recept bewerken' }).click();

  const dialog = page.locator('dialog[open]');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Titel behandeling')).toHaveValue('Volledige kleuring');

  await page.keyboard.press('Escape');
});

test('editing a recipe title and saving calls PUT and refreshes the list', async ({ page }) => {
  let putCalled = false;
  const updatedRecipe = {
    ...mockRecipeFull,
    title: 'Gedeeltelijke kleuring',
    updatedAtUtc: '2026-02-16T10:00:00Z',
    updatedBy: 'staff-1',
  };
  const updatedSummary = {
    ...mockRecipeSummary,
    title: 'Gedeeltelijke kleuring',
    updatedAtUtc: '2026-02-16T10:00:00Z',
  };

  await setupApiMocks(page);

  await page.route('**/api/recipes/recipe-1', (route) => {
    if (route.request().method() === 'PUT') {
      putCalled = true;
      return route.fulfill({ json: updatedRecipe });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // After save, the detail page reloads recipes
  let recipeCallCount = 0;
  await page.route('**/api/clients/client-1/recipes', (route) => {
    recipeCallCount++;
    if (route.request().method() === 'GET') {
      // After the first load, return updated data
      if (recipeCallCount > 1) {
        return route.fulfill({ json: [updatedSummary] });
      }
      return route.fulfill({ json: [mockRecipeSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/klanten/client-1');
  await expect(page.getByText('Volledige kleuring')).toBeVisible();

  await page.getByRole('button', { name: 'Recept bewerken' }).click();

  const dialog = page.locator('dialog[open]');
  await dialog.getByLabel('Titel behandeling').fill('Gedeeltelijke kleuring');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  expect(putCalled).toBe(true);
  await expect(page.getByText('Gedeeltelijke kleuring')).toBeVisible();
});

test('completed booking without recipe shows Recept toevoegen button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  await expect(page.getByRole('heading', { name: 'Afgeronde boekingen' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Recept toevoegen' })).toBeVisible();
});

test('clicking Recept toevoegen opens recipe form, saves, and shows in Behandelgeschiedenis', async ({
  page,
}) => {
  const newRecipe = {
    id: 'recipe-2',
    bookingId: 'booking-2',
    clientId: 'client-1',
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

  const newRecipeSummary = {
    id: 'recipe-2',
    bookingId: 'booking-2',
    bookingDate: '2026-03-01T14:00:00Z',
    staffMemberId: 'staff-1',
    staffMemberName: 'Jan Jansen',
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
    updatedAtUtc: null,
  };

  let postCalled = false;
  await setupApiMocks(page);

  await page.route('**/api/recipes', (route) => {
    if (route.request().method() === 'POST') {
      postCalled = true;
      return route.fulfill({ json: newRecipe, status: 201 });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  // After save, the detail page reloads recipes and bookings
  let recipeCallCount = 0;
  await page.route('**/api/clients/client-1/recipes', (route) => {
    recipeCallCount++;
    if (route.request().method() === 'GET') {
      if (recipeCallCount > 1) {
        return route.fulfill({ json: [mockRecipeSummary, newRecipeSummary] });
      }
      return route.fulfill({ json: [mockRecipeSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/klanten/client-1');

  // The completed booking without recipe should show the add button
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

  // Save
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  expect(postCalled).toBe(true);
  // Verify the new recipe appears in Behandelgeschiedenis
  await expect(page.getByText('Knippen standaard')).toBeVisible();
});

test('Terug naar klanten link navigates back to the clients list', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/klanten/client-1');

  await page.getByRole('link', { name: /Terug naar klanten/ }).click();
  await expect(page).toHaveURL(/\/klanten$/);
});
