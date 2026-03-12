import { Route } from '@angular/router';

import { ShellComponent } from '@org/shared-lib';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'diensten', pathMatch: 'full' },
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: 'boekingen',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.bookingsRoutes),
      },
      {
        path: 'diensten',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.servicesRoutes),
      },
      {
        path: 'klanten',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.clientsRoutes),
      },
      {
        path: 'facturen',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.billingRoutes),
      },
      {
        path: 'medewerkers',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.staffRoutes),
      },
      {
        path: 'meldingen',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.notificationsRoutes),
      },
      {
        path: 'instellingen',
        loadChildren: () => import('@org/chairly-lib').then((m) => m.settingsRoutes),
      },
    ],
  },
];
