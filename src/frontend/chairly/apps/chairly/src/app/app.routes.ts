import { Route } from '@angular/router';

import { ShellComponent } from '@org/shared-lib';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'diensten', pathMatch: 'full' },
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: 'diensten',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.servicesRoutes),
      },
      {
        path: 'klanten',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.clientsRoutes),
      },
      {
        path: 'medewerkers',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.staffRoutes),
      },
    ],
  },
];
