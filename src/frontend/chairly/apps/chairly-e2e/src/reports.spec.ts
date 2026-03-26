import { expect, test } from './fixtures';

const mockRevenueReport = {
  periodType: 'week',
  periodStart: '2026-03-23',
  periodEnd: '2026-03-29',
  salonName: 'Salon Test',
  rows: [
    {
      date: '2026-03-23',
      invoiceNumber: '2026-0042',
      totalAmount: 65.0,
      vatAmount: 11.3,
      paymentMethod: 'Pin',
    },
    {
      date: '2026-03-23',
      invoiceNumber: '2026-0043',
      totalAmount: 45.0,
      vatAmount: 7.82,
      paymentMethod: 'Cash',
    },
    {
      date: '2026-03-24',
      invoiceNumber: '2026-0044',
      totalAmount: 120.0,
      vatAmount: 20.83,
      paymentMethod: 'BankTransfer',
    },
  ],
  dailyTotals: [
    { date: '2026-03-23', totalAmount: 110.0, vatAmount: 19.12, invoiceCount: 2 },
    { date: '2026-03-24', totalAmount: 120.0, vatAmount: 20.83, invoiceCount: 1 },
  ],
  grandTotal: { totalAmount: 230.0, vatAmount: 39.95, invoiceCount: 3 },
};

const mockEmptyReport = {
  periodType: 'week',
  periodStart: '2026-03-23',
  periodEnd: '2026-03-29',
  salonName: 'Salon Test',
  rows: [],
  dailyTotals: [],
  grandTotal: { totalAmount: 0, vatAmount: 0, invoiceCount: 0 },
};

async function setupReportMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/reports/revenue/pdf*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        contentType: 'application/pdf',
        body: Buffer.from('%PDF-1.4 minimal'),
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/reports/revenue*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockRevenueReport });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('revenue report page loads with data', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByRole('heading', { name: 'Omzetrapport', level: 1 })).toBeVisible();
  await expect(page.getByText('Salon Test')).toBeVisible();
  await expect(page.getByText('2026-0042')).toBeVisible();
  await expect(page.getByText('2026-0043')).toBeVisible();
  await expect(page.getByText('2026-0044')).toBeVisible();
});

test('daily subtotals are shown', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByText('Subtotaal').first()).toBeVisible();
});

test('grand total is shown', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByText('Totaal').first()).toBeVisible();
  await expect(page.getByText('3 facturen').first()).toBeVisible();
});

test('period toggle switches between week and month', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await page.getByRole('button', { name: 'Maand' }).click();

  await expect(page).toHaveURL(/periode=month/);
});

test('period toggle switches to year', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await page.getByRole('button', { name: 'Jaar' }).click();

  await expect(page).toHaveURL(/periode=year/);
});

test('payment methods display in Dutch', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByText('Pin').first()).toBeVisible();
  await expect(page.getByText('Contant')).toBeVisible();
  await expect(page.getByText('Overboeking')).toBeVisible();
});

test('PDF download button is present', async ({ page }) => {
  await setupReportMocks(page);
  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByRole('button', { name: 'PDF downloaden' })).toBeVisible();
});

test('empty state when no invoices', async ({ page }) => {
  await page.route('**/api/reports/revenue*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockEmptyReport });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/rapporten?periode=week&datum=2026-03-23');

  await expect(page.getByText('Geen betaalde facturen in deze periode.')).toBeVisible();
});
