import { Route } from '@angular/router';

import { InvoiceApiService, InvoiceStore } from './data-access';
import { InvoiceDetailPageComponent, InvoiceListPageComponent } from './feature';

// eslint-disable-next-line sonarjs/todo-tag -- tracked requirement
// TODO: Add canActivate/canMatch route guard for Owner/Manager only access (spec F2)
export const billingRoutes: Route[] = [
  {
    path: '',
    component: InvoiceListPageComponent,
    providers: [InvoiceStore, InvoiceApiService],
  },
  {
    path: ':id',
    component: InvoiceDetailPageComponent,
    providers: [InvoiceStore, InvoiceApiService],
  },
];
