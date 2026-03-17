import { test as base } from '@playwright/test';

/**
 * Create a mock JWT token (unsigned) with the given payload claims.
 * keycloak-js only base64-decodes the payload — it does not verify signatures.
 */
function createMockJwt(claims: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify(claims)).toString('base64url');
  return `${header}.${payload}.`;
}

const BASE_TOKEN_CLAIMS = {
  exp: 9999999999,
  iat: Math.floor(Date.now() / 1000),
  sub: 'e2e-user',
  sid: 'mock-session-id',
  realm_access: { roles: ['owner', 'manager', 'staff_member'] },
  resource_access: {},
  given_name: 'Test',
  family_name: 'User',
};

export const test = base.extend({
  page: async ({ page }, use) => {
    // Shared state between route handlers: the nonce from the auth request
    // must appear in the ID token so keycloak-js nonce validation passes.
    let capturedNonce = '';

    // ---------------------------------------------------------------
    // Playwright routes match in LIFO order (last registered wins).
    // Register catch-all / low-priority routes FIRST, then specific
    // routes AFTER so they take priority.
    // ---------------------------------------------------------------

    // 1. API catch-all — lowest priority for /api/** routes.
    await page.route('**/api/**', (route) => {
      const url = route.request().url();
      console.warn(`[e2e] Un-mocked API call intercepted: ${route.request().method()} ${url}`);
      return route.fulfill({
        status: 599,
        contentType: 'application/json',
        body: JSON.stringify({ error: `Un-mocked API route: ${url}` }),
      });
    });

    // 2. Keycloak catch-all — lowest priority for /keycloak-mock/** routes.
    //    Handles unknown keycloak requests (e.g. login-status-iframe).
    await page.route('**/keycloak-mock/**', (route) => {
      const url = route.request().url();
      if (url.includes('login-status-iframe')) {
        return route.fulfill({
          status: 200,
          contentType: 'text/html',
          body: '<html><body></body></html>',
        });
      }
      return route.abort();
    });

    // 3. /api/config — higher priority than API catch-all.
    await page.route('**/api/config', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          keycloakUrl: 'http://localhost:4200/keycloak-mock',
          keycloakRealm: 'test',
          keycloakClientId: 'test',
        }),
      }),
    );

    // --- Keycloak OIDC mock endpoints (higher priority than catch-all) ---
    //
    // keycloak-js is bundled via ES module import and cannot be monkey-patched
    // via window.Keycloak. Instead we intercept the actual HTTP requests:
    //
    //   a) Auth redirect: browser navigates to the authorize endpoint.
    //      We capture the nonce and redirect back with a mock authorization code.
    //   b) Token exchange: keycloak-js POSTs to the token endpoint.
    //      We return mock JWT tokens (unsigned — keycloak-js does not verify).
    //      The ID token includes the captured nonce for nonce validation.
    //   c) Account endpoint: keycloak-js GETs /account for loadUserProfile().

    // 4. Token exchange endpoint.
    await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/token', (route) => {
      const accessToken = createMockJwt({ ...BASE_TOKEN_CLAIMS });
      const refreshToken = createMockJwt({ ...BASE_TOKEN_CLAIMS, typ: 'Refresh' });
      const idToken = createMockJwt({
        ...BASE_TOKEN_CLAIMS,
        typ: 'ID',
        nonce: capturedNonce,
      });

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          access_token: accessToken,
          refresh_token: refreshToken,
          id_token: idToken,
          token_type: 'Bearer',
          expires_in: 86400,
          refresh_expires_in: 86400,
          session_state: 'mock-session-id',
        }),
      });
    });

    // 5. Account endpoint (loadUserProfile).
    await page.route('**/keycloak-mock/realms/test/account', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          firstName: 'Test',
          lastName: 'User',
          email: 'test@example.com',
          username: 'e2e-user',
        }),
      }),
    );

    // 6. Auth redirect — highest priority keycloak route (registered last).
    //    keycloak-js navigates the browser here when onLoad is 'login-required'.
    //    We extract state, nonce, and redirect_uri, then redirect back with a
    //    mock auth code. We use an HTML page with JS redirect because
    //    Playwright's route.fulfill() with status 302 does not reliably trigger
    //    browser-level redirects.
    await page.route('**/keycloak-mock/realms/test/protocol/openid-connect/auth**', (route) => {
      const requestUrl = new URL(route.request().url());
      const state = requestUrl.searchParams.get('state') ?? 'mock-state';
      const nonce = requestUrl.searchParams.get('nonce') ?? '';
      const redirectUri = requestUrl.searchParams.get('redirect_uri') ?? 'http://localhost:4200/';
      const responseMode = requestUrl.searchParams.get('response_mode') ?? 'fragment';

      // Store nonce so the token endpoint can include it in the ID token.
      capturedNonce = nonce;

      let callbackUrl: string;

      if (responseMode === 'query') {
        const url = new URL(redirectUri);
        url.searchParams.set('state', state);
        url.searchParams.set('session_state', 'mock-session-id');
        url.searchParams.set('code', 'mock-auth-code');
        callbackUrl = url.toString();
      } else {
        // Default: fragment — append auth params as hash
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

    await use(page);
  },
});

export { expect } from '@playwright/test';
