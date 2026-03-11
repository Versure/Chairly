import { Route } from '@angular/router';

import { SettingsApiService } from './data-access';
import { CompanyInfoPageComponent, VatSettingsPageComponent } from './feature';

export const settingsRoutes: Route[] = [
  {
    path: '',
    component: CompanyInfoPageComponent,
    providers: [SettingsApiService],
  },
  {
    path: 'btw',
    component: VatSettingsPageComponent,
    providers: [SettingsApiService],
  },
];
