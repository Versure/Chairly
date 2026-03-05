import { Route } from '@angular/router';

import { StaffApiService, StaffStore } from './data-access';
import { StaffListPageComponent } from './feature';

export const staffRoutes: Route[] = [
  {
    path: '',
    component: StaffListPageComponent,
    providers: [StaffStore, StaffApiService],
  },
];
