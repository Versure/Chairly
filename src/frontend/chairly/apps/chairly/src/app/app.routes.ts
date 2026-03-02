import { Route } from '@angular/router';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'services', pathMatch: 'full' },
  {
    path: 'services',
    loadChildren: () => import('@org/chairly-lib').then((m) => m.servicesRoutes),
  },
];
