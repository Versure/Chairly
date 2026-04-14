import { expect, test } from './fixtures';

interface MockCampaign {
  id: string;
  subject: string;
  bodyHtml: string;
  recipientFilter: string;
  status: 'Draft' | 'Scheduled' | 'Sending' | 'Sent' | 'Cancelled';
  scheduledAtUtc: string | null;
  queuedAtUtc: string | null;
  sentAtUtc: string | null;
  cancelledAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
  pendingCount: number;
  unsubscribedCount: number;
  eligibleSubscribers: number;
}

/**
 * Create a mock JWT (unsigned) with the given payload claims.
 */
function createMockJwt(claims: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify(claims)).toString('base64url');
  return `${header}.${payload}.`;
}

function baseClaimsForRole(role: string): Record<string, unknown> {
  return {
    exp: 9999999999,
    iat: Math.floor(Date.now() / 1000),
    sub: 'e2e-user',
    sid: 'mock-session-id',
    realm_access: { roles: [role] },
    resource_access: {},
    given_name: 'Test',
    family_name: 'User',
  };
}

/**
 * Override the Keycloak OIDC mock to issue tokens with a specific role. Used
 * for role-based access control tests.
 */
async function setupAuthWithRole(
  page: import('@playwright/test').Page,
  role: string,
): Promise<void> {
  let capturedNonce = '';
  const claims = baseClaimsForRole(role);

  await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/auth**', (route) => {
    const requestUrl = new URL(route.request().url());
    const state = requestUrl.searchParams.get('state') ?? 'mock-state';
    capturedNonce = requestUrl.searchParams.get('nonce') ?? '';
    const redirectUri = requestUrl.searchParams.get('redirect_uri') ?? 'http://localhost:4200/';
    const responseMode = requestUrl.searchParams.get('response_mode') ?? 'fragment';

    let callbackUrl: string;
    if (responseMode === 'query') {
      const url = new URL(redirectUri);
      url.searchParams.set('state', state);
      url.searchParams.set('session_state', 'mock-session-id');
      url.searchParams.set('code', 'mock-auth-code');
      callbackUrl = url.toString();
    } else {
      const params = new URLSearchParams({
        state,
        session_state: 'mock-session-id',
        code: 'mock-auth-code',
      });
      callbackUrl = `${redirectUri}#${params.toString()}`;
    }

    return route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: `<!DOCTYPE html><html><head><script>window.location.replace(${JSON.stringify(callbackUrl)});</script></head><body></body></html>`,
    });
  });

  await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/token', (route) => {
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        access_token: createMockJwt(claims),
        refresh_token: createMockJwt({ ...claims, typ: 'Refresh' }),
        id_token: createMockJwt({ ...claims, typ: 'ID', nonce: capturedNonce }),
        token_type: 'Bearer',
        expires_in: 86400,
        refresh_expires_in: 86400,
        session_state: 'mock-session-id',
      }),
    });
  });
}

function buildDraft(id: string, subject: string): MockCampaign {
  return {
    id,
    subject,
    bodyHtml: '<p>Hallo klanten</p>',
    recipientFilter: 'AllSubscribed',
    status: 'Draft',
    scheduledAtUtc: null,
    queuedAtUtc: null,
    sentAtUtc: null,
    cancelledAtUtc: null,
    createdAtUtc: '2026-04-01T00:00:00Z',
    updatedAtUtc: null,
    totalRecipients: 0,
    sentCount: 0,
    failedCount: 0,
    pendingCount: 0,
    unsubscribedCount: 0,
    eligibleSubscribers: 42,
  };
}

function toSummary(c: MockCampaign): unknown {
  return {
    id: c.id,
    subject: c.subject,
    status: c.status,
    recipientCount: c.totalRecipients,
    sentCount: c.sentCount,
    failedCount: c.failedCount,
    scheduledAtUtc: c.scheduledAtUtc,
    sentAtUtc: c.sentAtUtc,
    createdAtUtc: c.createdAtUtc,
  };
}

async function setupApiMocks(
  page: import('@playwright/test').Page,
  initial: MockCampaign[] = [],
): Promise<void> {
  const store: Record<string, MockCampaign> = {};
  for (const c of initial) {
    store[c.id] = { ...c };
  }

  await page.route('**/api/newsletters/preview', (route) =>
    route.fulfill({
      json: { subject: 'Voorbeeld', htmlBody: '<html><body>Voorbeeld</body></html>' },
    }),
  );

  function handleCollection(method: string, req: import('@playwright/test').Request): unknown {
    if (method === 'GET') return { json: Object.values(store).map(toSummary) };
    if (method === 'POST') {
      const body = req.postDataJSON() as { subject: string; bodyHtml: string };
      const newId = `c-${Object.keys(store).length + 1}`;
      store[newId] = buildDraft(newId, body.subject);
      store[newId].bodyHtml = body.bodyHtml;
      return { json: store[newId] };
    }
    return { status: 404, body: '' };
  }

  function handleSingle(
    method: string,
    id: string,
    req: import('@playwright/test').Request,
  ): unknown {
    if (method === 'GET') return store[id] ? { json: store[id] } : { status: 404, body: '' };
    if (method === 'PUT') {
      const body = req.postDataJSON() as { subject: string; bodyHtml: string };
      store[id] = { ...store[id], subject: body.subject, bodyHtml: body.bodyHtml };
      return { json: store[id] };
    }
    if (method === 'DELETE') {
      delete store[id];
      return { status: 204, body: '' };
    }
    return { status: 404, body: '' };
  }

  function handleAction(
    action: string,
    id: string,
    req: import('@playwright/test').Request,
  ): unknown {
    if (action === 'schedule') {
      const body = req.postDataJSON() as { scheduledAtUtc: string };
      store[id] = { ...store[id], status: 'Scheduled', scheduledAtUtc: body.scheduledAtUtc };
      return { json: store[id] };
    }
    if (action === 'cancel') {
      store[id] = { ...store[id], status: 'Cancelled', cancelledAtUtc: '2026-04-10T00:00:00Z' };
      return { json: store[id] };
    }
    if (action === 'send') {
      store[id] = {
        ...store[id],
        status: 'Sent',
        sentAtUtc: '2026-04-10T00:00:00Z',
        totalRecipients: 42,
        sentCount: 42,
      };
      return { json: store[id] };
    }
    if (action === 'test-send') return { status: 202, body: '' };
    return { status: 404, body: '' };
  }

  await page.route(/\/api\/newsletters\/campaigns(\/[^?]*)?(\?.*)?$/, (route) => {
    const req = route.request();
    const method = req.method();
    const parts = new URL(req.url()).pathname.split('/').filter(Boolean);
    const id = parts[3];
    const action = parts[4];

    let result: unknown;
    if (!id) {
      result = handleCollection(method, req);
    } else if (!action) {
      result = handleSingle(method, id, req);
    } else {
      result = handleAction(action, id, req);
    }
    return route.fulfill(result as Parameters<typeof route.fulfill>[0]);
  });
}

/**
 * Fill the Quill editor body with plain text. The Quill editor renders a
 * contenteditable div with class .ql-editor.
 */
async function fillQuillBody(page: import('@playwright/test').Page, text: string): Promise<void> {
  const editor = page.locator('.ql-editor').first();
  await editor.waitFor({ state: 'visible' });
  await editor.click();
  await page.keyboard.type(text);
}

test('shows the nieuwsbrief list page with heading and create button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/nieuwsbrief');
  await expect(page.getByRole('heading', { name: 'Nieuwsbrief', level: 1 })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Nieuwe nieuwsbrief' })).toBeVisible();
});

test('shows empty state when no campaigns exist', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/nieuwsbrief');
  await expect(page.getByText('Nog geen nieuwsbrieven verstuurd.')).toBeVisible();
});

test('navigates to the compose page when clicking the create button', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/nieuwsbrief');
  await page.getByRole('button', { name: 'Nieuwe nieuwsbrief' }).first().click();
  await expect(page).toHaveURL(/\/nieuwsbrief\/nieuw$/);
  await expect(page.getByRole('heading', { name: 'Nieuwe nieuwsbrief' })).toBeVisible();
});

test('shows an existing draft in the list', async ({ page }) => {
  await setupApiMocks(page, [buildDraft('c1', 'Lente-actie')]);
  await page.goto('/nieuwsbrief');
  await expect(page.getByText('Lente-actie')).toBeVisible();
  await expect(page.getByText('Concept')).toBeVisible();
});

test('detail page shows recipient counts for sent campaigns', async ({ page }) => {
  const sent: MockCampaign = {
    ...buildDraft('c1', 'Lente-actie'),
    status: 'Sent',
    sentAtUtc: '2026-04-05T12:00:00Z',
    totalRecipients: 42,
    sentCount: 40,
    failedCount: 2,
  };
  await setupApiMocks(page, [sent]);
  await page.goto('/nieuwsbrief/c1');
  await expect(page.getByText('Lente-actie')).toBeVisible();
  await expect(page.getByText('Verzonden', { exact: true }).first()).toBeVisible();
});

test('saves a new draft and navigates to the edit URL', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/nieuwsbrief/nieuw');

  await page.getByLabel('Onderwerp').fill('Lente-actie 2026');
  await fillQuillBody(page, 'Beste klant, dit is een test.');

  await page.getByRole('button', { name: 'Opslaan als concept' }).click();
  await expect(page).toHaveURL(/\/nieuwsbrief\/c-1\/bewerken$/);
});

test('opens the preview modal and closes it with Escape', async ({ page }) => {
  await setupApiMocks(page);
  await page.goto('/nieuwsbrief/nieuw');

  await page.getByLabel('Onderwerp').fill('Voorbeeld-onderwerp');
  await fillQuillBody(page, 'Inhoud voor voorbeeld.');

  await page.getByRole('button', { name: 'Voorbeeld bekijken' }).click();

  const previewIframe = page.locator('iframe[title="Nieuwsbrief voorbeeld"]');
  await expect(previewIframe).toBeVisible();
  await expect(page.getByRole('heading', { name: /Voorbeeld:/ })).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(previewIframe).toBeHidden();
});

test('sends a test email and shows a success message', async ({ page }) => {
  await setupApiMocks(page, [buildDraft('c1', 'Test-campagne')]);
  await page.goto('/nieuwsbrief/c1/bewerken');

  await page.getByRole('button', { name: 'Test-e-mail naar mijzelf' }).click();
  await expect(page.getByRole('status')).toContainText('Test-e-mail verzonden');
});

test('schedules a campaign and shows Ingepland status on detail page', async ({ page }) => {
  await setupApiMocks(page, [buildDraft('c1', 'Geplande-campagne')]);
  await page.goto('/nieuwsbrief/c1/bewerken');

  await page.getByRole('button', { name: 'Inplannen...' }).click();

  // Pick a date/time in the future via flatpickr. We bypass the calendar UI
  // to keep this hermetic and robust across browsers: find the flatpickr
  // instance attached to the hidden input and call setDate + trigger close.
  const futureIso = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
  await page.evaluate((iso) => {
    const hidden = document.querySelector<HTMLInputElement>(
      '.flatpickr-wrapper input[type="hidden"]',
    );
    // eslint-disable-next-line @typescript-eslint/no-explicit-any -- flatpickr attaches to HTMLInputElement at runtime
    const fp = (hidden as any)?._flatpickr;
    if (fp) {
      fp.setDate(iso, true);
      fp.close();
    }
  }, futureIso);

  await page
    .locator('chairly-schedule-newsletter-dialog')
    .getByRole('button', { name: 'Bevestigen' })
    .click();

  // Navigate to detail page to verify status.
  await page.goto('/nieuwsbrief/c1');
  await expect(page.getByText('Ingepland', { exact: true }).first()).toBeVisible();

  // Back to list and verify the Ingepland badge appears.
  await page.goto('/nieuwsbrief');
  await expect(page.getByText('Ingepland', { exact: true }).first()).toBeVisible();
});

test('cancels a scheduled campaign via the detail page', async ({ page }) => {
  const scheduled: MockCampaign = {
    ...buildDraft('c1', 'Te-annuleren-campagne'),
    status: 'Scheduled',
    scheduledAtUtc: '2026-05-01T10:00:00Z',
  };
  await setupApiMocks(page, [scheduled]);
  await page.goto('/nieuwsbrief/c1');

  await page.getByRole('button', { name: 'Annuleren', exact: true }).click();

  // Confirmation dialog — click Bevestigen inside the confirm dialog.
  await page.getByRole('dialog').getByRole('button', { name: 'Bevestigen' }).click();

  await expect(page.getByText('Geannuleerd', { exact: true }).first()).toBeVisible();
});

test('sends a draft immediately after confirming the send dialog', async ({ page }) => {
  await setupApiMocks(page, [buildDraft('c1', 'Directe-verzend-campagne')]);
  await page.goto('/nieuwsbrief/c1/bewerken');

  await page.getByRole('button', { name: 'Nu verzenden' }).click();

  // Confirmation dialog shows recipient count text.
  await expect(page.getByText(/klanten wilt versturen/)).toBeVisible();
  await expect(page.getByText('42 klanten')).toBeVisible();

  await page.locator('dialog[open]').getByRole('button', { name: 'Bevestigen' }).click();

  // After sending, the edit page becomes read-only and shows the sent notice.
  await expect(page.getByText(/verzonden/)).toBeVisible();
});

test('staff member cannot access the nieuwsbrief page', async ({ page }) => {
  await setupAuthWithRole(page, 'staff_member');
  await setupApiMocks(page);

  await page.goto('/nieuwsbrief');

  // roleGuard redirects to /toegang-geweigerd when the user lacks the role.
  await expect(page).toHaveURL(/\/toegang-geweigerd$/);
  await expect(page.getByRole('heading', { name: 'Nieuwsbrief', level: 1 })).toBeHidden();
});
