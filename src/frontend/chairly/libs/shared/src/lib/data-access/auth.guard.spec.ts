import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
import { KeycloakService } from 'keycloak-angular';

import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let keycloakServiceMock: {
    isLoggedIn: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    keycloakServiceMock = {
      isLoggedIn: vi.fn(),
      login: vi.fn().mockResolvedValue(undefined),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        // eslint-disable-next-line sonarjs/deprecation -- KeycloakService mock for testing
        { provide: KeycloakService, useValue: keycloakServiceMock },
      ],
    });
  });

  it('returns true when user is logged in', () => {
    keycloakServiceMock.isLoggedIn.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('redirects to login when not logged in', () => {
    keycloakServiceMock.isLoggedIn.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toBe(false);
    expect(keycloakServiceMock.login).toHaveBeenCalled();
  });
});

describe('roleGuard', () => {
  let keycloakServiceMock: {
    getUserRoles: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(() => {
    keycloakServiceMock = {
      getUserRoles: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        // eslint-disable-next-line sonarjs/deprecation -- KeycloakService mock for testing
        { provide: KeycloakService, useValue: keycloakServiceMock },
      ],
    });

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('returns true when user has the required role', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['owner', 'manager', 'staff_member']);

    const { roleGuard: roleGuardFn } = await import('./role.guard');
    const guard = roleGuardFn('owner');

    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('redirects to /toegang-geweigerd when user lacks the role', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['staff_member']);

    const { roleGuard: roleGuardFn } = await import('./role.guard');
    const guard = roleGuardFn('owner');

    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));

    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/toegang-geweigerd']);
  });
});
