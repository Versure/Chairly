import { Route } from '@angular/router';

import {
  ServiceApiService,
  ServiceCategoryApiService,
  ServiceCategoryStore,
  ServiceStore,
} from '../data-access';
import { ServiceListPageComponent } from './service-list-page.component';

export const servicesRoutes: Route[] = [
  {
    path: '',
    component: ServiceListPageComponent,
    providers: [ServiceStore, ServiceCategoryStore, ServiceApiService, ServiceCategoryApiService],
  },
];
