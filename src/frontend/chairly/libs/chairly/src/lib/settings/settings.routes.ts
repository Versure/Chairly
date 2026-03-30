import { Route } from '@angular/router';

import { SettingsApiService } from './data-access';
import {
  EmailTemplateEditPageComponent,
  EmailTemplatesPageComponent,
  SettingsPageComponent,
} from './feature';

export const settingsRoutes: Route[] = [
  {
    path: '',
    component: SettingsPageComponent,
    providers: [SettingsApiService],
  },
  {
    path: 'email-templates',
    component: EmailTemplatesPageComponent,
  },
  {
    path: 'email-templates/:templateType',
    component: EmailTemplateEditPageComponent,
  },
];
