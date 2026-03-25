import { Route } from '@angular/router';

import { ReportApiService, ReportStore } from './data-access';
import { RevenueReportPageComponent } from './feature';

export const reportsRoutes: Route[] = [
  {
    path: '',
    component: RevenueReportPageComponent,
    providers: [ReportStore, ReportApiService],
  },
];
