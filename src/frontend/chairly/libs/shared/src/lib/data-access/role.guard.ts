import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

export function roleGuard(requiredRole: string): CanActivateFn {
  return (): boolean => {
    // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
    const keycloakService = inject(KeycloakService);
    const router = inject(Router);

    const userRoles = keycloakService.getUserRoles();

    if (userRoles.includes(requiredRole)) {
      return true;
    }

    void router.navigate(['/toegang-geweigerd']);
    return false;
  };
}
