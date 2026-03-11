import { expect, test } from './fixtures';

const mockVatSettings = {
  defaultVatRate: 21,
};

async function setupApiMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/settings/vat', (route) => {
    const method = route.request().method();
    if (method === 'GET') {
      return route.fulfill({ json: mockVatSettings });
    }
    if (method === 'PUT') {
      const body = route.request().postDataJSON() as { defaultVatRate: number };
      return route.fulfill({ json: { defaultVatRate: body.defaultVatRate } });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigates to /instellingen/btw and shows the BTW-instellingen heading', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen/btw');

  await expect(page.getByRole('heading', { name: 'BTW-instellingen', level: 1 })).toBeVisible();
  await expect(
    page.getByText('Het standaard BTW-tarief wordt automatisch toegepast'),
  ).toBeVisible();
});

test('displays the default VAT rate select with 21% selected', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen/btw');

  const select = page.getByLabel('Standaard BTW-tarief');
  await expect(select).toBeVisible();
  // With [ngValue] Angular uses internal indexing; check the visible selected text
  await expect(select.locator('option:checked')).toHaveText('21%');
});

test('changing default VAT to 9% and clicking Opslaan shows success banner', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen/btw');

  const select = page.getByLabel('Standaard BTW-tarief');
  await expect(select).toBeVisible();

  await select.selectOption({ label: '9%' });

  await page.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Instellingen opgeslagen')).toBeVisible();
});

test('sidebar contains Instellingen link', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen/btw');

  const link = page.getByRole('link', { name: 'Instellingen' });
  await expect(link).toBeVisible();
});
