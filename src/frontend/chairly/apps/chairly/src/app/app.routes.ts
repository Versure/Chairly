import { Route } from '@angular/router';

import { authGuard, roleGuard, ShellComponent } from '@org/shared-lib';

import { AccessDeniedComponent } from './access-denied/access-denied.component';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'diensten', pathMatch: 'full' },
  { path: 'toegang-geweigerd', component: AccessDeniedComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
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
        canActivate: [roleGuard('manager')],
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
        canActivate: [roleGuard('owner')],
        loadChildren: () => import('@org/chairly-lib').then((m) => m.settingsRoutes),
      },
    ],
  },
];
