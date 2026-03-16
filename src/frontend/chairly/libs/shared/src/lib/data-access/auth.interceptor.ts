import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';

import { from, switchMap } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  if (req.url.includes('/api/config')) {
    return next(req);
  }

  // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
  const keycloakService = inject(KeycloakService);

  return from(keycloakService.getToken()).pipe(
    switchMap((token) => {
      if (token) {
        const authReq = req.clone({
          setHeaders: { Authorization: `Bearer ${token}` },
        });
        return next(authReq);
      }
      return next(req);
    }),
  );
};
