import { Route } from '@angular/router';

import { NewslettersApiService, NewsletterStore } from './data-access';

export const newslettersRoutes: Route[] = [
  {
    path: '',
    providers: [NewsletterStore, NewslettersApiService],
    children: [
      {
        path: '',
        loadComponent: () => import('./feature').then((m) => m.NewsletterListPageComponent),
      },
      {
        path: 'nieuw',
        loadComponent: () => import('./feature').then((m) => m.NewsletterEditPageComponent),
      },
      {
        path: ':id',
        loadComponent: () => import('./feature').then((m) => m.NewsletterDetailPageComponent),
      },
      {
        path: ':id/bewerken',
        loadComponent: () => import('./feature').then((m) => m.NewsletterEditPageComponent),
      },
    ],
  },
];
