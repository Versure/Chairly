import { Route } from '@angular/router';

import { ClientStore } from './data-access';
import { ClientDetailPageComponent, ClientListPageComponent } from './feature';

export const clientsRoutes: Route[] = [
  {
    path: '',
    component: ClientListPageComponent,
    providers: [ClientStore],
  },
  {
    path: ':clientId',
    component: ClientDetailPageComponent,
  },
];
