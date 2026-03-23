import { expect, test } from './fixtures';

const mockListResponse = {
  items: [
    {
      id: '00000000-0000-0000-0000-000000000001',
      salonName: 'Salon De Schaar',
      ownerName: 'Jan Jansen',
      email: 'jan@deschaar.nl',
      plan: 'starter',
      billingCycle: 'Monthly',
      isTrial: false,
      status: 'provisioned',
      createdAtUtc: '2026-01-15T10:00:00Z',
      provisionedAtUtc: '2026-01-16T10:00:00Z',
      cancelledAtUtc: null,
    },
    {
      id: '00000000-0000-0000-0000-000000000002',
      salonName: 'Kapsalon Mooi',
      ownerName: 'Piet Pietersen',
      email: 'piet@mooi.nl',
      plan: 'team',
      billingCycle: null,
      isTrial: true,
      status: 'trial',
      createdAtUtc: '2026-02-01T08:00:00Z',
      provisionedAtUtc: null,
      cancelledAtUtc: null,
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 25,
};

const emptyListResponse = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 25,
};

test.describe('Subscription List', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockListResponse),
      }),
    );
  });

  test('should display page heading', async ({ page }) => {
    await page.goto('/abonnementen');
    // "Abonnementen" appears in sidebar nav link and page h1 — scope to main content h1
    await expect(page.locator('main h1')).toHaveText('Abonnementen');
  });

  test('should display subscription data in table', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();
    await expect(page.locator('table').getByText('Kapsalon Mooi')).toBeVisible();
    await expect(page.locator('table').getByText('Jan Jansen')).toBeVisible();
    await expect(page.locator('table').getByText('jan@deschaar.nl')).toBeVisible();
  });

  test('should show status badges', async ({ page }) => {
    await page.goto('/abonnementen');
    // Status badges are in table cells — scope to tbody to avoid select options
    await expect(page.locator('tbody').getByText('Actief')).toBeVisible();
    await expect(page.locator('tbody').getByText('Proefperiode')).toBeVisible();
  });

  test('should show empty state when no results', async ({ page }) => {
    await page.route('**/api/admin/subscriptions?**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(emptyListResponse),
      }),
    );
    await page.goto('/abonnementen');
    await expect(page.getByText('Geen abonnementen gevonden.')).toBeVisible();
  });

  test('should have search input with placeholder', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.getByPlaceholder('Zoeken op salonnaam, e-mail, naam...')).toBeVisible();
  });

  test('should have status and plan filter dropdowns', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('select').nth(0)).toBeVisible();
    await expect(page.locator('select').nth(1)).toBeVisible();
  });

  test('should show total count badge', async ({ page }) => {
    await page.goto('/abonnementen');
    // Total count badge is a span next to the main content h1
    await expect(page.locator('main h1 ~ span')).toBeVisible();
    await expect(page.locator('main h1 ~ span')).toHaveText('2');
  });

  test('should have "Bekijken" links', async ({ page }) => {
    await page.goto('/abonnementen');
    const links = page.getByText('Bekijken');
    await expect(links.first()).toBeVisible();
  });

  test('should trigger API call when typing in search input', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();

    const apiCalled = page.waitForRequest(
      (request) =>
        request.url().includes('/api/admin/subscriptions') && request.url().includes('search=Test'),
    );

    await page.getByPlaceholder('Zoeken op salonnaam, e-mail, naam...').fill('Test');
    await apiCalled;
  });

  test('should trigger API call when selecting status filter', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();

    const apiCalled = page.waitForRequest(
      (request) =>
        request.url().includes('/api/admin/subscriptions') &&
        request.url().includes('status=provisioned'),
    );

    await page.locator('select').nth(0).selectOption('provisioned');
    await apiCalled;
  });

  test('should trigger API call when selecting plan filter', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();

    const apiCalled = page.waitForRequest(
      (request) =>
        request.url().includes('/api/admin/subscriptions') && request.url().includes('plan=team'),
    );

    await page.locator('select').nth(1).selectOption('team');
    await apiCalled;
  });

  test('should navigate to detail page when clicking Bekijken', async ({ page }) => {
    // Mock the detail endpoint
    await page.route('**/api/admin/subscriptions/00000000-0000-0000-0000-000000000001', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '00000000-0000-0000-0000-000000000001',
          salonName: 'Salon De Schaar',
          ownerFirstName: 'Jan',
          ownerLastName: 'Jansen',
          email: 'jan@deschaar.nl',
          phoneNumber: null,
          plan: 'starter',
          billingCycle: 'Monthly',
          isTrial: false,
          status: 'provisioned',
          trialEndsAtUtc: null,
          createdAtUtc: '2026-01-15T10:00:00Z',
          createdByName: null,
          provisionedAtUtc: '2026-01-16T10:00:00Z',
          provisionedByName: null,
          cancelledAtUtc: null,
          cancelledByName: null,
          cancellationReason: null,
        }),
      }),
    );

    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();

    await page.getByText('Bekijken').first().click();
    await expect(page).toHaveURL(/\/abonnementen\/00000000-0000-0000-0000-000000000001/);
    await expect(page.locator('main h1')).toHaveText('Salon De Schaar');
  });

  test('should update URL query params when filters change', async ({ page }) => {
    await page.goto('/abonnementen');
    await expect(page.locator('table').getByText('Salon De Schaar')).toBeVisible();

    await page.locator('select').nth(0).selectOption('provisioned');
    await expect(page).toHaveURL(/status=provisioned/);

    await page.locator('select').nth(1).selectOption('team');
    await expect(page).toHaveURL(/plan=team/);
  });

  test('should navigate pages with Vorige/Volgende buttons', async ({ page }) => {
    // Mock a response with multiple pages
    await page.route('**/api/admin/subscriptions?**', (route) => {
      const url = new URL(route.request().url());
      const currentPage = Number(url.searchParams.get('page') ?? '1');
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: mockListResponse.items,
          totalCount: 100,
          page: currentPage,
          pageSize: 25,
        }),
      });
    });

    await page.goto('/abonnementen');
    await expect(page.getByText('Pagina 1 van 4')).toBeVisible();

    const nextButton = page.getByRole('button', { name: 'Volgende' });
    await expect(nextButton).toBeEnabled();

    const previousButton = page.getByRole('button', { name: 'Vorige' });
    await expect(previousButton).toBeDisabled();

    await nextButton.click();
    await expect(page.getByText('Pagina 2 van 4')).toBeVisible();
    await expect(previousButton).toBeEnabled();
  });
});
