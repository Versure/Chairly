import { Pipe, PipeTransform } from '@angular/core';

import { BookingStatus } from '../../models';

const STATUS_LABELS: Record<BookingStatus, string> = {
  Scheduled: 'Gepland',
  Confirmed: 'Bevestigd',
  InProgress: 'Bezig',
  Completed: 'Voltooid',
  Cancelled: 'Geannuleerd',
  NoShow: 'No-show',
};

@Pipe({
  name: 'bookingStatusLabel',
  standalone: true,
  pure: true,
})
export class BookingStatusLabelPipe implements PipeTransform {
  transform(status: BookingStatus): string {
    return STATUS_LABELS[status] ?? status;
  }
}
