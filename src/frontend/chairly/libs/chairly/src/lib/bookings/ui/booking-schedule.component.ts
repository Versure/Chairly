import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';

import { Booking } from '../models';
import {
  BookingStatusPipe,
  NameLookupPipe,
  ScheduleHeightPipe,
  ScheduleTopPipe,
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
    BookingStatusPipe,
    NameLookupPipe,
    ScheduleTopPipe,
    ScheduleHeightPipe,
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

  readonly bookingSelected: OutputEmitterRef<Booking> = output<Booking>();
  readonly statusAction: OutputEmitterRef<BookingStatusAction> = output<BookingStatusAction>();

  /** Trigger signal for the timeSlots pipe (pipes need at least one argument). */
  protected readonly slotTrigger = true;
}
