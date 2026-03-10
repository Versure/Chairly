import { Route } from '@angular/router';

import { BookingApiService, BookingReferenceDataService, BookingStore } from './data-access';
import { BookingListPageComponent } from './feature';

export const bookingsRoutes: Route[] = [
  {
    path: '',
    component: BookingListPageComponent,
    providers: [BookingStore, BookingApiService, BookingReferenceDataService],
  },
];
