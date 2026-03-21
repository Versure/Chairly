import { Route } from '@angular/router';

export const appRoutes: Route[] = [
  {
    path: '',
    loadChildren: () => import('@org/website-lib').then((m) => m.onboardingRoutes),
  },
];
