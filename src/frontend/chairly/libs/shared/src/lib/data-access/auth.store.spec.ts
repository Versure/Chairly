import { TestBed } from '@angular/core/testing';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
import { KeycloakService } from 'keycloak-angular';

import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: InstanceType<typeof AuthStore>;
  let keycloakServiceMock: {
    loadUserProfile: ReturnType<typeof vi.fn>;
    getUserRoles: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    keycloakServiceMock = {
      loadUserProfile: vi.fn().mockResolvedValue({
        firstName: 'Jan',
        lastName: 'de Vries',
      }),
      getUserRoles: vi.fn().mockReturnValue(['owner']),
      logout: vi.fn().mockResolvedValue(undefined),
    };

    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        // eslint-disable-next-line sonarjs/deprecation -- KeycloakService mock for testing
        { provide: KeycloakService, useValue: keycloakServiceMock },
      ],
    });

    store = TestBed.inject(AuthStore);
  });

  it('computes isManager correctly for owner role', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['owner']);

    store.loadUserProfile();
    await vi.waitFor(() => {
      expect(store.isLoading()).toBe(false);
    });

    expect(store.isOwner()).toBe(true);
    expect(store.isManager()).toBe(true);
  });

  it('computes isManager correctly for manager role', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['manager']);

    store.loadUserProfile();
    await vi.waitFor(() => {
      expect(store.isLoading()).toBe(false);
    });

    expect(store.isOwner()).toBe(false);
    expect(store.isManager()).toBe(true);
  });

  it('computes isManager as false for staff_member role', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['staff_member']);

    store.loadUserProfile();
    await vi.waitFor(() => {
      expect(store.isLoading()).toBe(false);
    });

    expect(store.isOwner()).toBe(false);
    expect(store.isManager()).toBe(false);
  });

  it('computes userFullName from loaded profile', async () => {
    store.loadUserProfile();
    await vi.waitFor(() => {
      expect(store.isLoading()).toBe(false);
    });

    expect(store.userFullName()).toBe('Jan de Vries');
  });

  it('loads user role from realm roles', async () => {
    keycloakServiceMock.getUserRoles.mockReturnValue(['some_other_role', 'manager']);

    store.loadUserProfile();
    await vi.waitFor(() => {
      expect(store.isLoading()).toBe(false);
    });

    expect(store.userRole()).toBe('manager');
  });
});
