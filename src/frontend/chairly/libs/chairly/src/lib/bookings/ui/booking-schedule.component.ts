import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  OutputEmitterRef,
} from '@angular/core';

import { Booking, ScheduleRange, StaffMemberOption } from '../models';
import {
  BookingOverlapPipe,
  BookingStatusPipe,
  NameLookupPipe,
  ScheduleHeightPipe,
  ScheduleTopPipe,
  StaffColorPipe,
  TimeSlotsPipe,
  TimeSlotTopPipe,
} from '../pipes';
import {
  BookingStatusAction,
  BookingStatusActionsComponent,
} from './booking-status-actions.component';

@Component({
  selector: 'chairly-booking-schedule',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    BookingOverlapPipe,
    BookingStatusPipe,
    NameLookupPipe,
    ScheduleTopPipe,
    ScheduleHeightPipe,
    StaffColorPipe,
    TimeSlotsPipe,
    TimeSlotTopPipe,
    BookingStatusActionsComponent,
  ],
  templateUrl: './booking-schedule.component.html',
})
export class BookingScheduleComponent {
  readonly bookings = input.required<Booking[]>();
  readonly clientNameMap = input<Record<string, string>>({});
  readonly staffMemberNameMap = input<Record<string, string>>({});
  readonly staffMembers = input<StaffMemberOption[]>([]);

  readonly bookingSelected: OutputEmitterRef<Booking> = output<Booking>();
  readonly statusAction: OutputEmitterRef<BookingStatusAction> = output<BookingStatusAction>();

  /** Dynamic schedule range computed from bookings, defaults to 08:00-20:00. */
  protected readonly scheduleRange = computed<ScheduleRange>(() => {
    const bookings = this.bookings();
    let minHour = 8;
    let maxHour = 20;

    for (const booking of bookings) {
      const start = new Date(booking.startTime);
      const end = new Date(booking.endTime);

      const startHour = start.getHours();
      const endHour = end.getMinutes() > 0 ? end.getHours() + 1 : end.getHours();

      if (startHour < minHour) {
        minHour = startHour;
      }
      if (endHour > maxHour) {
        maxHour = endHour;
      }
    }

    return { startHour: minHour, endHour: Math.min(maxHour, 24) };
  });

  /** Pixel height for the schedule container based on the time range. */
  protected readonly scheduleHeight = computed<number>(() => {
    const range = this.scheduleRange();
    const hours = range.endHour - range.startHour;
    return hours * 60; // 60px per hour
  });
}
