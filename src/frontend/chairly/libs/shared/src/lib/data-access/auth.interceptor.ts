import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';

import { catchError, from, Observable, switchMap, throwError } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

let hasTriggeredLoginRedirect = false;

export const resetAuthInterceptorStateForTests = (): void => {
  hasTriggeredLoginRedirect = false;
};

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  if (req.url.includes('/api/config')) {
    return next(req);
  }

  // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config
  const keycloakService = inject(KeycloakService);

  const withRetryHeader = req.headers.has('X-Auth-Retry');
  const triggerLoginIfNeeded = (error: HttpErrorResponse): Observable<never> => {
    if (!keycloakService.isLoggedIn() && !hasTriggeredLoginRedirect) {
      hasTriggeredLoginRedirect = true;
      void keycloakService.login({ redirectUri: window.location.href });
    }

    return throwError(() => error);
  };

  const withToken = (token: string | null, request: HttpRequest<unknown>): HttpRequest<unknown> => {
    if (!token) {
      return request;
    }

    return request.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  };

  return from(keycloakService.getToken()).pipe(
    switchMap((token) => next(withToken(token, req))),
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !withRetryHeader) {
        return from(keycloakService.updateToken(30)).pipe(
          switchMap(() => from(keycloakService.getToken())),
          switchMap((refreshedToken) =>
            next(
              withToken(
                refreshedToken,
                req.clone({
                  setHeaders: { 'X-Auth-Retry': '1' },
                }),
              ),
            ),
          ),
          catchError(() => {
            return triggerLoginIfNeeded(error);
          }),
        );
      }

      return throwError(() => error);
    }),
  );
};
