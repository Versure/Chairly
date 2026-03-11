import { Route } from '@angular/router';

import { SettingsApiService } from './data-access';
import { SettingsPageComponent } from './feature';

export const settingsRoutes: Route[] = [
  {
    path: '',
    component: SettingsPageComponent,
    providers: [SettingsApiService],
  },
];
