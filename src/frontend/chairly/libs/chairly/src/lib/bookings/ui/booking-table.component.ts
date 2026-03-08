import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';

import { Booking } from '../models';
import { BookingStatusPipe } from '../pipes';
import {
  BookingStatusAction,
  BookingStatusActionsComponent,
} from './booking-status-actions.component';

@Component({
  selector: 'chairly-booking-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, BookingStatusPipe, BookingStatusActionsComponent],
  templateUrl: './booking-table.component.html',
})
export class BookingTableComponent {
  readonly bookings = input.required<Booking[]>();

  readonly bookingSelected: OutputEmitterRef<Booking> = output<Booking>();
  readonly statusAction: OutputEmitterRef<BookingStatusAction> = output<BookingStatusAction>();
}
