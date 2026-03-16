import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { firstValueFrom } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
import { KeycloakService } from 'keycloak-angular';

import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;
  let keycloakServiceMock: { getToken: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    keycloakServiceMock = {
      getToken: vi.fn().mockResolvedValue('mock-token-123'),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        // eslint-disable-next-line sonarjs/deprecation -- KeycloakService mock for testing
        { provide: KeycloakService, useValue: keycloakServiceMock },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('adds Authorization header to non-config requests', async () => {
    const response$ = firstValueFrom(httpClient.get('/api/bookings'));

    // Wait a microtask for the from(promise) in the interceptor to resolve
    await Promise.resolve();

    const req = httpTesting.expectOne('/api/bookings');
    expect(req.request.headers.get('Authorization')).toBe('Bearer mock-token-123');
    req.flush([]);

    await response$;
  });

  it('does NOT add Authorization header to /api/config', async () => {
    const response$ = firstValueFrom(httpClient.get('/api/config'));

    const req = httpTesting.expectOne('/api/config');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});

    await response$;
  });
});
