import { Route } from '@angular/router';

import { SettingsApiService } from './data-access';
import { VatSettingsPageComponent } from './feature';

export const settingsRoutes: Route[] = [
  {
    path: '',
    children: [
      {
        path: 'btw',
        component: VatSettingsPageComponent,
        providers: [SettingsApiService],
      },
      { path: '', redirectTo: 'btw', pathMatch: 'full' },
    ],
  },
];
