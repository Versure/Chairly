import { Route } from '@angular/router';

import { authGuard } from '@org/shared-lib';

export const appRoutes: Route[] = [
  { path: '', redirectTo: 'abonnementen', pathMatch: 'full' },
  {
    path: '',
    loadComponent: () => import('@org/admin-lib').then((m) => m.AdminShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'abonnementen',
        loadChildren: () => import('@org/admin-lib').then((m) => m.subscriptionsRoutes),
      },
    ],
  },
];
