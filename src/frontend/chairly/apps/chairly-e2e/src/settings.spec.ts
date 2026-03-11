import { expect, test } from './fixtures';

const emptyCompanyInfo = {
  companyName: null,
  companyEmail: null,
  street: null,
  houseNumber: null,
  postalCode: null,
  city: null,
  companyPhone: null,
  ibanNumber: null,
  vatNumber: null,
  paymentPeriodDays: null,
};

const filledCompanyInfo = {
  companyName: 'Salon Mooi',
  companyEmail: 'info@salonmooi.nl',
  street: 'Kerkstraat',
  houseNumber: '1',
  postalCode: '1234 AB',
  city: 'Amsterdam',
  companyPhone: '020-1234567',
  ibanNumber: 'NL91ABNA0417164300',
  vatNumber: 'NL123456789B01',
  paymentPeriodDays: 30,
};

const mockVatSettings = {
  defaultVatRate: 21,
};

async function setupApiMocks(
  page: import('@playwright/test').Page,
  initialInfo = emptyCompanyInfo,
): Promise<void> {
  let currentInfo = { ...initialInfo };

  await page.route('**/api/settings/company', (route) => {
    const method = route.request().method();
    if (method === 'GET') {
      return route.fulfill({ json: currentInfo });
    }
    if (method === 'PUT') {
      const body = route.request().postDataJSON() as typeof currentInfo;
      currentInfo = { ...body };
      return route.fulfill({ json: currentInfo });
    }
    return route.fulfill({ status: 404, body: '' });
  });

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

// --- Combined Settings Page Tests ---

test('navigating to /instellingen shows h1 Instellingen', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
});

test('page shows both Bedrijfsinformatie and BTW-instellingen sections', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Bedrijfsinformatie', level: 2 })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'BTW-instellingen', level: 2 })).toBeVisible();
});

test('page shows description text about invoices', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(page.getByText('Deze gegevens worden gebruikt op uw facturen.')).toBeVisible();
});

test('page shows BTW description text', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(
    page.getByText('Het standaard BTW-tarief wordt automatisch toegepast'),
  ).toBeVisible();
});

test('fill in company name and email, click Opslaan, and verify success banner appears', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await page.getByLabel('Bedrijfsnaam').fill('Salon Mooi');
  await page.getByLabel('E-mailadres').fill('info@salonmooi.nl');

  // Click the first Opslaan button (company section)
  const companyForm = page.locator('form');
  await companyForm.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Bedrijfsinformatie opgeslagen')).toBeVisible();
});

test('sidebar contains Instellingen link', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  const navLink = page.getByRole('link', { name: 'Instellingen' });
  await expect(navLink).toBeVisible();
});

test('form fields retain values after page reload', async ({ page }) => {
  await setupApiMocks(page, filledCompanyInfo);
  await page.goto('/instellingen');

  await expect(page.getByLabel('Bedrijfsnaam')).toHaveValue('Salon Mooi');
  await expect(page.getByLabel('E-mailadres')).toHaveValue('info@salonmooi.nl');
  await expect(page.getByLabel('Straat')).toHaveValue('Kerkstraat');
  await expect(page.getByLabel('Huisnummer')).toHaveValue('1');
  await expect(page.getByLabel('Postcode')).toHaveValue('1234 AB');
  await expect(page.getByLabel('Plaats')).toHaveValue('Amsterdam');
  await expect(page.getByLabel('Telefoonnummer')).toHaveValue('020-1234567');
  await expect(page.getByLabel('IBAN-nummer')).toHaveValue('NL91ABNA0417164300');
  await expect(page.getByLabel('BTW-nummer')).toHaveValue('NL123456789B01');
  await expect(page.getByLabel('Betaaltermijn (dagen)')).toHaveValue('30');
});

test('clicking Instellingen in sidebar navigates to /instellingen', async ({ page }) => {
  await setupApiMocks(page);
  // Start from a different page
  await page.route('**/api/service-categories', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/services', (route) => route.fulfill({ json: [] }));
  await page.goto('/diensten');

  const navLink = page.getByRole('link', { name: 'Instellingen' });
  await expect(navLink).toBeVisible();

  await navLink.click();
  await expect(page).toHaveURL(/\/instellingen/);
  await expect(page.getByRole('heading', { name: 'Instellingen', level: 1 })).toBeVisible();
});

test('company success banner disappears after a few seconds', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await page.getByLabel('Bedrijfsnaam').fill('Test Salon');

  const companyForm = page.locator('form');
  await companyForm.getByRole('button', { name: 'Opslaan' }).click();

  const banner = page.getByText('Bedrijfsinformatie opgeslagen');
  await expect(banner).toBeVisible();

  // Wait for auto-dismiss (3 seconds + buffer)
  await expect(banner).toBeHidden({ timeout: 5000 });
});

// --- VAT Settings Tests (on same page) ---

test('displays the default VAT rate select with 21% selected', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  const select = page.getByLabel('Standaard BTW-tarief');
  await expect(select).toBeVisible();
  // With [ngValue] Angular uses internal indexing; check the visible selected text
  await expect(select.locator('option:checked')).toHaveText('21%');
});

test('changing default VAT to 9% and clicking Opslaan shows success banner', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  const select = page.getByLabel('Standaard BTW-tarief');
  await expect(select).toBeVisible();

  await select.selectOption({ label: '9%' });

  // Click the VAT section's Opslaan button (not inside the form element)
  const vatSection = page.locator('section').filter({ hasText: 'BTW-instellingen' });
  await vatSection.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('BTW-instellingen opgeslagen')).toBeVisible();
});
