import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { firstValueFrom } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
import { KeycloakService } from 'keycloak-angular';

import { authInterceptor, resetAuthInterceptorStateForTests } from './auth.interceptor';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;
  let keycloakServiceMock: {
    getToken: ReturnType<typeof vi.fn>;
    updateToken: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
    isLoggedIn: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    resetAuthInterceptorStateForTests();

    keycloakServiceMock = {
      getToken: vi.fn().mockResolvedValue('mock-token-123'),
      updateToken: vi.fn().mockResolvedValue(true),
      login: vi.fn().mockResolvedValue(undefined),
      isLoggedIn: vi.fn().mockReturnValue(true),
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

  const flushMicrotasks = async (times = 1): Promise<void> => {
    for (let index = 0; index < times; index += 1) {
      await Promise.resolve();
    }
  };

  it('adds Authorization header to non-config requests', async () => {
    const response$ = firstValueFrom(httpClient.get('/api/bookings'));

    // Wait microtasks for the from(promise) in the interceptor to resolve
    await flushMicrotasks(2);

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

  it('retries once after 401 by refreshing the token', async () => {
    keycloakServiceMock.getToken
      .mockResolvedValueOnce('expired-token')
      .mockResolvedValueOnce('fresh-token');

    const responsePromise = firstValueFrom(httpClient.post('/api/clients', { firstName: 'Anna' }));
    await flushMicrotasks(2);

    const firstReq = httpTesting.expectOne('/api/clients');
    expect(firstReq.request.headers.get('Authorization')).toBe('Bearer expired-token');
    expect(firstReq.request.headers.has('X-Auth-Retry')).toBe(false);
    firstReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    await flushMicrotasks(4);

    const retryReq = httpTesting.expectOne('/api/clients');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer fresh-token');
    expect(retryReq.request.headers.get('X-Auth-Retry')).toBe('1');
    retryReq.flush({ id: 'client-1' }, { status: 201, statusText: 'Created' });

    await responsePromise;
    expect(keycloakServiceMock.updateToken).toHaveBeenCalledWith(30);
    expect(keycloakServiceMock.login).not.toHaveBeenCalled();
  });

  it('redirects to login when refresh fails after 401', async () => {
    keycloakServiceMock.getToken.mockResolvedValue('expired-token');
    keycloakServiceMock.updateToken.mockRejectedValue(new Error('refresh failed'));
    keycloakServiceMock.isLoggedIn = vi.fn().mockReturnValue(false);

    const responsePromise = firstValueFrom(httpClient.post('/api/clients', { firstName: 'Anna' }));
    await flushMicrotasks(2);

    const req = httpTesting.expectOne('/api/clients');
    req.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    await flushMicrotasks(3);
    await expect(responsePromise).rejects.toBeTruthy();
    expect(keycloakServiceMock.login).toHaveBeenCalled();
  });

  it('does not trigger repeated login redirects after refresh failures', async () => {
    keycloakServiceMock.getToken.mockResolvedValue('expired-token');
    keycloakServiceMock.updateToken.mockRejectedValue(new Error('refresh failed'));
    keycloakServiceMock.isLoggedIn = vi.fn().mockReturnValue(false);

    const firstResponsePromise = firstValueFrom(httpClient.post('/api/clients', { firstName: 'Anna' }));
    await flushMicrotasks(2);
    const firstReq = httpTesting.expectOne('/api/clients');
    firstReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await flushMicrotasks(3);
    await expect(firstResponsePromise).rejects.toBeTruthy();

    const secondResponsePromise = firstValueFrom(httpClient.post('/api/clients', { firstName: 'Anna' }));
    await flushMicrotasks(2);
    const secondReq = httpTesting.expectOne('/api/clients');
    secondReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    await flushMicrotasks(3);
    await expect(secondResponsePromise).rejects.toBeTruthy();

    expect(keycloakServiceMock.login).toHaveBeenCalledTimes(1);
  });
});
