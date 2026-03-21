import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import localeNl from '@angular/common/locales/nl';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { appRoutes } from './app.routes';

registerLocaleData(localeNl);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(appRoutes),
    provideHttpClient(),
    { provide: LOCALE_ID, useValue: 'nl-NL' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
  ],
};
