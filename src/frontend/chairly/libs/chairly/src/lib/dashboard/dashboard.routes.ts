import { Route } from '@angular/router';

import { DashboardStore } from './data-access';
import { DashboardPageComponent } from './feature';

export const dashboardRoutes: Route[] = [
  {
    path: '',
    component: DashboardPageComponent,
    providers: [DashboardStore],
  },
];
