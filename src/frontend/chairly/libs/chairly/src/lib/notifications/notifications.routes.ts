import { Route } from '@angular/router';

import { NotificationsApiService, NotificationStore } from './data-access';
import { NotificationLogPageComponent } from './feature';

// eslint-disable-next-line sonarjs/todo-tag -- tracked requirement
// TODO: Add canActivate/canMatch route guard for Owner/Manager only access (spec F2)
export const notificationsRoutes: Route[] = [
  {
    path: '',
    component: NotificationLogPageComponent,
    providers: [NotificationStore, NotificationsApiService],
  },
];
