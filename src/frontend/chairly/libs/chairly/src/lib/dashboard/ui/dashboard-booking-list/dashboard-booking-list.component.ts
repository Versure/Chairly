import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { DashboardBooking } from '../../models';
import { DashboardBookingStatusPipe, JoinPipe } from '../../pipes';

@Component({
  selector: 'chairly-dashboard-booking-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DashboardBookingStatusPipe, JoinPipe, RouterLink],
  templateUrl: './dashboard-booking-list.component.html',
})
export class DashboardBookingListComponent {
  readonly title = input.required<string>();
  readonly bookings = input.required<DashboardBooking[]>();
  readonly emptyMessage = input.required<string>();
}
