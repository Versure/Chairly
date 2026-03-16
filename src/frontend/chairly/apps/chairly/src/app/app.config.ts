import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeNl from '@angular/common/locales/nl';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  inject,
  LOCALE_ID,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { firstValueFrom, from, switchMap } from 'rxjs';
// eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
import { KeycloakService } from 'keycloak-angular';

import { AppConfigService, authInterceptor, AuthStore } from '@org/shared-lib';

import { appRoutes } from './app.routes';

registerLocaleData(localeNl);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: LOCALE_ID, useValue: 'nl-NL' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
    // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
    KeycloakService,
    provideAppInitializer(() => {
      const appConfigService = inject(AppConfigService);
      // eslint-disable-next-line sonarjs/deprecation -- KeycloakService is required for runtime config; provideKeycloak needs static config at build time
      const keycloak = inject(KeycloakService);
      const authStore = inject(AuthStore);

      return firstValueFrom(
        appConfigService.load().pipe(
          switchMap((config) =>
            from(
              keycloak.init({
                config: {
                  url: config.keycloakUrl,
                  realm: config.keycloakRealm,
                  clientId: config.keycloakClientId,
                },
                initOptions: {
                  onLoad: 'login-required',
                  checkLoginIframe: false,
                },
              }),
            ),
          ),
        ),
      ).then((authenticated) => {
        if (authenticated) {
          authStore.loadUserProfile();
        }
      });
    }),
  ],
};
