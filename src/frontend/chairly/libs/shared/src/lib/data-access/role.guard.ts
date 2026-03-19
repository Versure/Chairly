import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

const roleHierarchy: Record<string, string[]> = {
  owner: ['owner'],
  manager: ['owner', 'manager'],
  staff_member: ['owner', 'manager', 'staff_member'],
};

function hasRequiredRole(userRoles: string[], requiredRole: string): boolean {
  const allowedRoles = roleHierarchy[requiredRole] ?? [requiredRole];
  return allowedRoles.some((role) => userRoles.includes(role));
}

export function roleGuard(requiredRole: string): CanActivateFn {
  return (): boolean => {
    // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
    const keycloakService = inject(KeycloakService);
    const router = inject(Router);

    const userRoles = keycloakService.getUserRoles();

    if (hasRequiredRole(userRoles, requiredRole)) {
      return true;
    }

    void router.navigate(['/toegang-geweigerd']);
    return false;
  };
}
