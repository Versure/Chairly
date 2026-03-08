import { Pipe, PipeTransform } from '@angular/core';

import { BookingStatus } from '../models';

const STATUS_LABELS: Record<BookingStatus, string> = {
  Scheduled: 'Gepland',
  Confirmed: 'Bevestigd',
  InProgress: 'Bezig',
  Completed: 'Voltooid',
  Cancelled: 'Geannuleerd',
  NoShow: 'Niet-verschenen',
};

@Pipe({
  name: 'bookingStatus',
  standalone: true,
})
export class BookingStatusPipe implements PipeTransform {
  transform(status: BookingStatus): string {
    return STATUS_LABELS[status] ?? status;
  }
}
