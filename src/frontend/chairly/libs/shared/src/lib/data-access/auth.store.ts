import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { from, take } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

export interface AuthState {
  firstName: string;
  lastName: string;
  userRole: string;
  isLoading: boolean;
}

const initialState: AuthState = {
  firstName: '',
  lastName: '',
  userRole: '',
  isLoading: false,
};

const KNOWN_ROLES = ['owner', 'manager', 'staff_member'];

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState<AuthState>(initialState),
  withComputed((store) => ({
    userFullName: computed(() => `${store.firstName()} ${store.lastName()}`.trim()),
    isOwner: computed(() => store.userRole() === 'owner'),
    isManager: computed(() => store.userRole() === 'owner' || store.userRole() === 'manager'),
  })),
  withMethods((store) => {
    // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
    const keycloakService = inject(KeycloakService);

    return {
      loadUserProfile(): void {
        patchState(store, { isLoading: true });

        from(keycloakService.loadUserProfile())
          .pipe(take(1))
          .subscribe({
            next: (profile) => {
              const roles = keycloakService.getUserRoles();
              const matchedRole = roles.find((r) => KNOWN_ROLES.includes(r)) ?? '';

              patchState(store, {
                firstName: profile.firstName ?? '',
                lastName: profile.lastName ?? '',
                userRole: matchedRole,
                isLoading: false,
              });
            },
            error: () => {
              patchState(store, { isLoading: false });
            },
          });
      },
      logout(): void {
        void keycloakService.logout(window.location.origin);
      },
    };
  }),
);
