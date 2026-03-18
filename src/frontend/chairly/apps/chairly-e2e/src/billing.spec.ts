import { expect, test } from './fixtures';

const mockInvoiceSummary = {
  id: 'inv-1',
  invoiceNumber: '2026-0001',
  invoiceDate: '2026-03-10',
  bookingId: 'bk-1',
  clientId: 'cl-1',
  clientFullName: 'Jan de Vries',
  totalAmount: 65,
  status: 'Concept',
  createdAtUtc: '2026-03-10T10:00:00Z',
  sentAtUtc: null,
  paidAtUtc: null,
  voidedAtUtc: null,
};

const mockInvoiceDetail = {
  ...mockInvoiceSummary,
  clientSnapshot: {
    fullName: 'Jan de Vries',
    email: 'jan@example.com',
    phone: '0612345678',
    address: null,
  },
  staffMemberName: 'Anna de Vries',
  subTotalAmount: 53.72,
  totalVatAmount: 11.28,
  lineItems: [
    {
      id: 'li-1',
      description: 'Herenknippen',
      quantity: 1,
      unitPrice: 25,
      totalPrice: 25,
      vatPercentage: 21,
      vatAmount: 5.25,
      isManual: false,
      sortOrder: 0,
    },
    {
      id: 'li-2',
      description: 'Baard trimmen',
      quantity: 1,
      unitPrice: 40,
      totalPrice: 40,
      vatPercentage: 21,
      vatAmount: 8.4,
      isManual: false,
      sortOrder: 1,
    },
  ],
};

const mockCompanyInfo = {
  companyName: 'Salon Chairly',
  companyEmail: 'info@chairly.nl',
  street: 'Keizersgracht',
  houseNumber: '42',
  postalCode: '1015 CR',
  city: 'Amsterdam',
  companyPhone: '020-1234567',
  ibanNumber: 'NL91ABNA0417164300',
  vatNumber: 'NL123456789B01',
  paymentPeriodDays: 30,
};

async function setupInvoiceListMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockInvoiceSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

async function setupCompanyInfoMock(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/settings/company', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockCompanyInfo });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

async function setupInvoiceDetailMocks(page: import('@playwright/test').Page): Promise<void> {
  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockInvoiceDetail });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockInvoiceSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
}

test('invoice list page shows heading and empty state when no invoices', async ({ page }) => {
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen');

  await expect(page.getByRole('heading', { name: 'Facturen', level: 1 })).toBeVisible();
  await expect(page.getByText('Geen facturen gevonden')).toBeVisible();
});

test('invoice list page shows invoices in a table with status badge', async ({ page }) => {
  await setupInvoiceListMocks(page);
  await page.goto('/facturen');

  await expect(page.getByRole('table')).toBeVisible();
  await expect(page.getByText('2026-0001')).toBeVisible();
  await expect(page.getByText('Jan de Vries')).toBeVisible();
  await expect(page.locator('td').getByText('Concept', { exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Bekijken' })).toBeVisible();
});

test('clicking Bekijken navigates to invoice detail page', async ({ page }) => {
  await setupInvoiceListMocks(page);
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen');

  await page.getByRole('link', { name: 'Bekijken' }).click();

  await expect(page).toHaveURL(/\/facturen\/inv-1/);
  await expect(page.getByRole('heading', { name: /Factuur 2026-0001/ })).toBeVisible();
});

test('invoice detail page shows line items and status history', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await expect(page.getByText('Herenknippen')).toBeVisible();
  await expect(page.getByText('Baard trimmen')).toBeVisible();
  await expect(page.getByText('Aangemaakt op')).toBeVisible();
  await expect(page.getByText('Jan de Vries')).toBeVisible();
});

test('clicking Factuur versturen updates status badge to Verzonden', async ({ page }) => {
  const sentInvoice = {
    ...mockInvoiceDetail,
    status: 'Verzonden',
    sentAtUtc: '2026-03-10T12:00:00Z',
  };

  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockInvoiceDetail });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices/inv-1/send', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: sentInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockInvoiceSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');
  await expect(
    page.locator('span.rounded-full').getByText('Concept', { exact: true }),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Factuur versturen' }).click();

  await expect(
    page.locator('span.rounded-full').getByText('Verzonden', { exact: true }),
  ).toBeVisible();
});

test('clicking Markeer als betaald updates status badge to Betaald', async ({ page }) => {
  const sentInvoice = {
    ...mockInvoiceDetail,
    status: 'Verzonden',
    sentAtUtc: '2026-03-10T12:00:00Z',
  };
  const paidInvoice = {
    ...sentInvoice,
    status: 'Betaald',
    paidAtUtc: '2026-03-10T14:00:00Z',
  };

  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: sentInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices/inv-1/pay', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: paidInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');
  await expect(
    page.locator('span.rounded-full').getByText('Verzonden', { exact: true }),
  ).toBeVisible();

  await page.getByRole('button', { name: 'Markeer als betaald' }).click();

  await expect(
    page.locator('span.rounded-full').getByText('Betaald', { exact: true }),
  ).toBeVisible();
});

test('Vervallen verklaren button is not shown on a paid invoice', async ({ page }) => {
  const paidInvoice = {
    ...mockInvoiceDetail,
    status: 'Betaald',
    sentAtUtc: '2026-03-10T12:00:00Z',
    paidAtUtc: '2026-03-10T14:00:00Z',
  };

  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: paidInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');
  await expect(
    page.locator('span.rounded-full').getByText('Betaald', { exact: true }),
  ).toBeVisible();

  await expect(page.getByRole('button', { name: 'Vervallen verklaren' })).toBeHidden();
  await expect(page.getByRole('button', { name: 'Factuur versturen' })).toBeHidden();
});

test('back link navigates to invoice list', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await page.getByRole('link', { name: /Terug naar facturen/ }).click();

  await expect(page).toHaveURL(/\/facturen$/);
});

// --- Invoice document layout tests ---

test('invoice detail page shows company information in header', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await expect(page.getByText('Salon Chairly')).toBeVisible();
  await expect(page.getByText('info@chairly.nl')).toBeVisible();
  await expect(page.getByText('Keizersgracht 42')).toBeVisible();
  await expect(page.getByText('1015 CR Amsterdam')).toBeVisible();
});

test('invoice detail page shows client information in header', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await expect(
    page.locator('.invoice-document').getByText('Jan de Vries', { exact: true }),
  ).toBeVisible();
  await expect(page.getByText('jan@example.com')).toBeVisible();
  await expect(page.getByText('0612345678')).toBeVisible();
});

test('invoice detail page shows invoice metadata', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await expect(page.getByText('Factuurnummer:')).toBeVisible();
  await expect(page.locator('.invoice-document').getByText('2026-0001')).toBeVisible();
  await expect(page.getByText('Factuurdatum:')).toBeVisible();
  await expect(page.getByText('Medewerker:')).toBeVisible();
  await expect(page.getByText('Anna de Vries')).toBeVisible();
});

test('invoice detail page shows footer with IBAN and BTW-nummer', async ({ page }) => {
  await setupInvoiceDetailMocks(page);
  await page.goto('/facturen/inv-1');

  await expect(page.getByText('NL91ABNA0417164300')).toBeVisible();
  await expect(page.getByText('NL123456789B01')).toBeVisible();
  await expect(page.getByText('30 dagen')).toBeVisible();
});

// --- F4: Generate invoice from completed booking ---

const mockCompletedBooking = {
  id: 'booking-1',
  clientId: 'cl-1',
  staffMemberId: 'staff-1',
  startTime: '2026-03-10T10:00:00Z',
  endTime: '2026-03-10T10:30:00Z',
  notes: null,
  status: 'Completed',
  services: [
    {
      serviceId: 'svc-1',
      serviceName: 'Herenknippen',
      duration: '00:30:00',
      price: 25,
      sortOrder: 0,
    },
  ],
  createdAtUtc: '2026-03-08T08:00:00Z',
  updatedAtUtc: null,
  confirmedAtUtc: '2026-03-08T09:00:00Z',
  startedAtUtc: '2026-03-10T10:00:00Z',
  completedAtUtc: '2026-03-10T10:30:00Z',
  cancelledAtUtc: null,
  noShowAtUtc: null,
};

const mockGeneratedInvoice = {
  id: 'inv-generated',
  invoiceNumber: '2026-0002',
  invoiceDate: '2026-03-10',
  bookingId: 'booking-1',
  clientId: 'cl-1',
  clientFullName: 'Jan de Vries',
  clientSnapshot: {
    fullName: 'Jan de Vries',
    email: 'jan@example.com',
    phone: '0612345678',
    address: null,
  },
  staffMemberName: 'Anna de Vries',
  subTotalAmount: 20.66,
  totalVatAmount: 4.34,
  totalAmount: 25,
  status: 'Concept',
  createdAtUtc: '2026-03-10T10:35:00Z',
  sentAtUtc: null,
  paidAtUtc: null,
  voidedAtUtc: null,
};

async function setupBookingPageMocks(page: import('@playwright/test').Page): Promise<void> {
  await page.route('**/api/bookings*', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockCompletedBooking] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/clients', (route) => {
    return route.fulfill({ json: [{ id: 'cl-1', firstName: 'Jan', lastName: 'de Vries' }] });
  });
  await page.route('**/api/staff', (route) => {
    return route.fulfill({ json: [{ id: 'staff-1', firstName: 'Anna', lastName: 'de Vries' }] });
  });
  await page.route('**/api/services', (route) => {
    return route.fulfill({
      json: [{ id: 'svc-1', name: 'Herenknippen', duration: '00:30:00', price: 25 }],
    });
  });
}

test('completed booking shows Factuur genereren button', async ({ page }) => {
  await setupBookingPageMocks(page);
  await page.goto('/boekingen');

  await expect(page.getByRole('button', { name: 'Factuur genereren' })).toBeVisible();
});

test('clicking Factuur genereren shows success message and Factuur bekijken link', async ({
  page,
}) => {
  await setupBookingPageMocks(page);
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: mockGeneratedInvoice, status: 201 });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/boekingen');
  await page.getByRole('button', { name: 'Factuur genereren' }).click();

  await expect(page.getByText('Factuur succesvol aangemaakt')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Factuur bekijken' })).toBeVisible();
});

test('clicking Factuur bekijken navigates to the generated invoice detail page', async ({
  page,
}) => {
  await setupBookingPageMocks(page);
  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: mockGeneratedInvoice, status: 201 });
    }
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockGeneratedInvoice] });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices/inv-generated', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({
        json: {
          ...mockGeneratedInvoice,
          lineItems: [
            {
              id: 'li-gen-1',
              description: 'Herenknippen',
              quantity: 1,
              unitPrice: 25,
              totalPrice: 25,
              vatPercentage: 21,
              vatAmount: 5.25,
              isManual: false,
              sortOrder: 0,
            },
          ],
        },
      });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/boekingen');
  await page.getByRole('button', { name: 'Factuur genereren' }).click();
  await expect(page.getByText('Factuur succesvol aangemaakt')).toBeVisible();

  await page.getByRole('button', { name: 'Factuur bekijken' }).click();

  await expect(page).toHaveURL(/\/facturen\/inv-generated/);
  await expect(page.getByRole('heading', { name: /Factuur 2026-0002/ })).toBeVisible();
});

// --- F4: Invoice regenerate e2e tests ---

const mockConceptInvoiceForRegenerate = {
  ...mockInvoiceDetail,
  subTotalAmount: 53.72,
  totalVatAmount: 11.28,
  totalAmount: 65,
  lineItems: [
    {
      id: 'li-1',
      description: 'Herenknippen',
      quantity: 1,
      unitPrice: 25,
      totalPrice: 25,
      vatPercentage: 21,
      vatAmount: 5.25,
      isManual: false,
      sortOrder: 0,
    },
    {
      id: 'li-2',
      description: 'Baard trimmen',
      quantity: 1,
      unitPrice: 40,
      totalPrice: 40,
      vatPercentage: 21,
      vatAmount: 8.4,
      isManual: false,
      sortOrder: 1,
    },
  ],
};

const mockRegeneratedInvoice = {
  ...mockConceptInvoiceForRegenerate,
  lineItems: [
    ...mockConceptInvoiceForRegenerate.lineItems,
    {
      id: 'li-3',
      description: 'Haarkleuring',
      quantity: 1,
      unitPrice: 50,
      totalPrice: 50,
      vatPercentage: 21,
      vatAmount: 10.5,
      isManual: false,
      sortOrder: 2,
    },
  ],
  subTotalAmount: 94.21,
  totalVatAmount: 20.79,
  totalAmount: 115,
};

test('Factuur opnieuw genereren button is visible on Concept invoice', async ({ page }) => {
  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockConceptInvoiceForRegenerate });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockInvoiceSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');

  await expect(page.getByRole('button', { name: 'Factuur opnieuw genereren' })).toBeVisible();
});

test('Factuur opnieuw genereren button is hidden on Verzonden invoice', async ({ page }) => {
  const sentInvoice = {
    ...mockConceptInvoiceForRegenerate,
    status: 'Verzonden',
    sentAtUtc: '2026-03-10T12:00:00Z',
  };

  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: sentInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');

  await expect(page.getByRole('button', { name: 'Factuur opnieuw genereren' })).toBeHidden();
});

test('Factuur opnieuw genereren button is hidden on Betaald invoice', async ({ page }) => {
  const paidInvoice = {
    ...mockConceptInvoiceForRegenerate,
    status: 'Betaald',
    sentAtUtc: '2026-03-10T12:00:00Z',
    paidAtUtc: '2026-03-10T14:00:00Z',
  };

  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: paidInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');

  await expect(page.getByRole('button', { name: 'Factuur opnieuw genereren' })).toBeHidden();
});

test('clicking Factuur opnieuw genereren updates line items', async ({ page }) => {
  await setupCompanyInfoMock(page);
  await page.route('**/api/invoices/inv-1', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: mockConceptInvoiceForRegenerate });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices/inv-1/regenerate', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: mockRegeneratedInvoice });
    }
    return route.fulfill({ status: 404, body: '' });
  });
  await page.route('**/api/invoices', (route) => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: [mockInvoiceSummary] });
    }
    return route.fulfill({ status: 404, body: '' });
  });

  await page.goto('/facturen/inv-1');
  await expect(page.getByText('Herenknippen')).toBeVisible();
  await expect(page.getByText('Baard trimmen')).toBeVisible();

  await page.getByRole('button', { name: 'Factuur opnieuw genereren' }).click();

  await expect(page.getByText('Haarkleuring')).toBeVisible();
});
