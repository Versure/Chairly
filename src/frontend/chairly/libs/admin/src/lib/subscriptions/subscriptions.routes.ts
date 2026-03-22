import { Route } from '@angular/router';

import { AdminSubscriptionApiService, AdminSubscriptionStore } from './data-access';
import { SubscriptionDetailPageComponent, SubscriptionListPageComponent } from './feature';

export const subscriptionsRoutes: Route[] = [
  {
    path: '',
    providers: [AdminSubscriptionStore, AdminSubscriptionApiService],
    children: [
      { path: '', component: SubscriptionListPageComponent },
      { path: ':id', component: SubscriptionDetailPageComponent },
    ],
  },
];
