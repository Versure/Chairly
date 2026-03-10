import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Booking } from '../models';

export interface BookingStatusAction {
  action: 'confirm' | 'start' | 'complete' | 'cancel' | 'noShow' | 'generateInvoice';
  bookingId: string;
}

@Component({
  selector: 'chairly-booking-status-actions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './booking-status-actions.component.html',
})
export class BookingStatusActionsComponent {
  readonly booking = input.required<Booking>();

  readonly action: OutputEmitterRef<BookingStatusAction> = output<BookingStatusAction>();

  protected emitAction(actionType: BookingStatusAction['action']): void {
    this.action.emit({ action: actionType, bookingId: this.booking().id });
  }
}
