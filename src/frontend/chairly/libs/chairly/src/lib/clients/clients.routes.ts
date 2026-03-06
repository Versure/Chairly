import { Route } from '@angular/router';

import { ClientStore } from './data-access';
import { ClientListPageComponent } from './feature';

export const clientsRoutes: Route[] = [
  {
    path: '',
    component: ClientListPageComponent,
    providers: [ClientStore],
  },
];
