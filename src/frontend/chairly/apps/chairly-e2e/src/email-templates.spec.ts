import { expect, test } from './fixtures';

const defaultTemplates = [
  {
    templateType: 'BookingConfirmation',
    subject: 'Bevestiging van uw afspraak bij Salon Mooi',
    mainMessage: 'Uw afspraak is bevestigd.',
    closingMessage: 'Wij kijken ernaar uit u te verwelkomen!',
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date', 'services'],
  },
  {
    templateType: 'BookingReminder',
    subject: 'Herinnering: uw afspraak morgen bij Salon Mooi',
    mainMessage: 'Dit is een herinnering dat u morgen een afspraak heeft.',
    closingMessage: 'Wij zien u graag!',
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date', 'services'],
  },
  {
    templateType: 'BookingCancellation',
    subject: 'Uw afspraak is geannuleerd',
    mainMessage: 'Uw afspraak is helaas geannuleerd.',
    closingMessage: 'Neem gerust contact met ons op als u een nieuwe afspraak wilt maken.',
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date'],
  },
  {
    templateType: 'BookingReceived',
    subject: 'Nieuwe boeking bij Salon Mooi',
    mainMessage: 'Wij hebben uw boeking ontvangen. Uw boeking wacht op bevestiging.',
    closingMessage: 'Wij nemen zo snel mogelijk contact met u op.',
    isCustomized: false,
    availablePlaceholders: ['clientName', 'salonName', 'date', 'services'],
  },
  {
    templateType: 'InvoiceSent',
    subject: 'Factuur F-2026-001 van Salon Mooi',
    mainMessage: 'Bedankt voor uw bezoek! Bijgaand vindt u uw factuur.',
    closingMessage: 'Wij zien u graag terug!',
    isCustomized: false,
    availablePlaceholders: [
      'clientName',
      'salonName',
      'invoiceNumber',
      'invoiceDate',
      'totalAmount',
    ],
  },
];

const mockPreviewResponse = {
  subject: 'Bevestiging van uw afspraak bij Salon Mooi',
  htmlBody: '<html><body><h1>Beste Jan de Vries</h1><p>Uw afspraak is bevestigd.</p></body></html>',
};

async function setupEmailTemplateMocks(page: import('@playwright/test').Page): Promise<void> {
  let templates = [...defaultTemplates];

  await page.route('**/api/notifications/email-templates/preview', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: mockPreviewResponse });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/notifications/email-templates/*', (route) => {
    const url = route.request().url();
    const method = route.request().method();

    // Extract templateType from the URL
    const parts = url.split('/email-templates/');
    const templateType = parts[1]?.split('?')[0];

    if (method === 'PUT' && templateType) {
      const body = route.request().postDataJSON() as {
        subject: string;
        mainMessage: string;
        closingMessage: string;
      };
      const idx = templates.findIndex((t) => t.templateType === templateType);
      if (idx !== -1) {
        templates[idx] = {
          ...templates[idx],
          ...body,
          isCustomized: true,
        };
      }
      return route.fulfill({ json: templates[idx] });
    }

    if (method === 'DELETE' && templateType) {
      const idx = templates.findIndex((t) => t.templateType === templateType);
      if (idx !== -1) {
        templates[idx] = {
          ...defaultTemplates[idx],
          isCustomized: false,
        };
      }
      return route.fulfill({ status: 204, body: '' });
    }

    return route.fulfill({ status: 404, body: '' });
  });

  await page.route('**/api/notifications/email-templates', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: templates });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('navigate to /instellingen/email-templates shows page heading', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates');

  await expect(page.getByRole('heading', { name: 'E-mailtemplates', level: 1 })).toBeVisible();
});

test('all 5 template cards are displayed with correct Dutch labels', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates');

  await expect(page.getByText('Boekingsbevestiging')).toBeVisible();
  await expect(page.getByText('Boekingsherinnering')).toBeVisible();
  await expect(page.getByText('Boekingsannulering')).toBeVisible();
  await expect(page.getByText('Boeking ontvangen')).toBeVisible();
  await expect(page.getByText('Factuur verzonden')).toBeVisible();
});

test('default templates show Standaard badge', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates');

  const badges = page.getByText('Standaard', { exact: true });
  await expect(badges.first()).toBeVisible();
});

test('clicking Bewerken navigates to edit page with pre-filled values', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates');

  await page.getByRole('link', { name: 'Bewerken' }).first().click();
  await expect(page).toHaveURL(/\/instellingen\/email-templates\/BookingConfirmation/);
  await expect(page.getByRole('heading', { name: 'Boekingsbevestiging bewerken' })).toBeVisible();
  await expect(page.getByLabel('Onderwerp')).toHaveValue(
    'Bevestiging van uw afspraak bij Salon Mooi',
  );
});

test('edit subject and save shows success banner', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  await page.getByLabel('Onderwerp').fill('Aangepast onderwerp');
  await page.getByRole('button', { name: 'Opslaan' }).click();

  await expect(page.getByText('Template opgeslagen')).toBeVisible();
});

test('after saving, list shows Aangepast badge', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  await page.getByLabel('Onderwerp').fill('Aangepast onderwerp');
  await page.getByRole('button', { name: 'Opslaan' }).click();
  await expect(page.getByText('Template opgeslagen')).toBeVisible();

  await page.getByRole('link', { name: 'Terug naar overzicht' }).click();
  await expect(page).toHaveURL(/\/instellingen\/email-templates$/);
  await expect(page.getByText('Aangepast')).toBeVisible();
});

test('re-opening edit page shows previously saved custom subject', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  await page.getByLabel('Onderwerp').fill('Aangepast onderwerp');
  await page.getByRole('button', { name: 'Opslaan' }).click();
  await expect(page.getByText('Template opgeslagen')).toBeVisible();

  await page.getByRole('link', { name: 'Terug naar overzicht' }).click();
  await expect(page).toHaveURL(/\/instellingen\/email-templates$/);

  await page.getByRole('link', { name: 'Bewerken' }).first().click();
  await expect(page).toHaveURL(/\/instellingen\/email-templates\/BookingConfirmation/);
  await expect(page.getByLabel('Onderwerp')).toHaveValue('Aangepast onderwerp');
});

test('clicking Voorbeeld bekijken opens preview modal with iframe', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  await page.getByRole('button', { name: 'Voorbeeld bekijken' }).click();

  await expect(page.getByRole('heading', { name: 'Voorbeeld e-mail' })).toBeVisible();
  await expect(page.locator('iframe[title="E-mail voorbeeld"]')).toBeVisible();
});

test('close preview modal with Escape key', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  await page.getByRole('button', { name: 'Voorbeeld bekijken' }).click();
  await expect(page.getByRole('heading', { name: 'Voorbeeld e-mail' })).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(page.getByRole('heading', { name: 'Voorbeeld e-mail' })).toBeHidden();
});

test('reset template shows confirmation and returns to Standaard badge', async ({ page }) => {
  await setupEmailTemplateMocks(page);
  // First, customize the template
  await page.goto('/instellingen/email-templates/BookingConfirmation');
  await page.getByLabel('Onderwerp').fill('Aangepast');
  await page.getByRole('button', { name: 'Opslaan' }).click();
  await expect(page.getByText('Template opgeslagen')).toBeVisible();

  // Navigate back and go to edit again
  await page.goto('/instellingen/email-templates/BookingConfirmation');

  // Click reset
  await page.getByRole('button', { name: 'Standaardwaarden herstellen' }).click();

  // Confirm the dialog
  await expect(page.getByText('Weet u zeker dat u dit template wilt herstellen')).toBeVisible();
  await page.getByRole('button', { name: 'Herstellen' }).click();

  // Should navigate back to list
  await expect(page).toHaveURL(/\/instellingen\/email-templates$/);

  // Verify the Standaard badge reappears
  await expect(page.getByText('Standaard', { exact: true }).first()).toBeVisible();
});
