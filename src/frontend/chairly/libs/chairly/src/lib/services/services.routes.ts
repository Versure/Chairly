import { Route } from '@angular/router';

import {
  ServiceApiService,
  ServiceCategoryApiService,
  ServiceCategoryStore,
  ServiceStore,
} from './data-access';
import { ServiceListPageComponent } from './feature';

export const servicesRoutes: Route[] = [
  {
    path: '',
    component: ServiceListPageComponent,
    providers: [ServiceStore, ServiceCategoryStore, ServiceApiService, ServiceCategoryApiService],
  },
];
