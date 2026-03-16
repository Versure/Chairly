import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

export const authGuard: CanActivateFn = (): boolean => {
  // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
  const keycloakService = inject(KeycloakService);

  if (keycloakService.isLoggedIn()) {
    return true;
  }

  void keycloakService.login();
  return false;
};
