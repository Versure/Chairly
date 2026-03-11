import { expect, test } from './fixtures';

const emptyCompanyInfo = {
  companyName: null,
  companyEmail: null,
  companyAddress: null,
  companyPhone: null,
  ibanNumber: null,
  vatNumber: null,
  paymentPeriodDays: null,
};

const filledCompanyInfo = {
  companyName: 'Salon Mooi',
  companyEmail: 'info@salonmooi.nl',
  companyAddress: 'Kerkstraat 1, 1234 AB Amsterdam',
  companyPhone: '020-1234567',
  ibanNumber: 'NL91ABNA0417164300',
  vatNumber: 'NL123456789B01',
  paymentPeriodDays: 30,
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
}

test('navigating to /instellingen shows h1 Bedrijfsinformatie', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(page.getByRole('heading', { name: 'Bedrijfsinformatie', level: 1 })).toBeVisible();
});

test('page shows description text about invoices', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await expect(page.getByText('Deze gegevens worden gebruikt op uw facturen.')).toBeVisible();
});

test('fill in company name and email, click Opslaan, and verify success banner appears', async ({
  page,
}) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await page.getByLabel('Bedrijfsnaam').fill('Salon Mooi');
  await page.getByLabel('E-mailadres').fill('info@salonmooi.nl');
  await page.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Instellingen opgeslagen')).toBeVisible();
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
  await expect(page.getByLabel('Adres', { exact: true })).toHaveValue('Kerkstraat 1, 1234 AB Amsterdam');
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
  await expect(page.getByRole('heading', { name: 'Bedrijfsinformatie', level: 1 })).toBeVisible();
});

test('success banner disappears after a few seconds', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/instellingen');

  await page.getByLabel('Bedrijfsnaam').fill('Test Salon');
  await page.getByRole('button', { name: 'Opslaan' }).click();

  const banner = page.getByText('Instellingen opgeslagen');
  await expect(banner).toBeVisible();

  // Wait for auto-dismiss (3 seconds + buffer)
  await expect(banner).toBeHidden({ timeout: 5000 });
});
