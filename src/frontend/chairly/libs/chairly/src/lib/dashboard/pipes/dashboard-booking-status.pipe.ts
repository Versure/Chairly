import { Pipe, PipeTransform } from '@angular/core';

const STATUS_LABELS: Record<string, string> = {
  Scheduled: 'Gepland',
  Confirmed: 'Bevestigd',
  InProgress: 'Bezig',
  Completed: 'Voltooid',
  Cancelled: 'Geannuleerd',
  NoShow: 'Niet-verschenen',
};

@Pipe({
  name: 'dashboardBookingStatus',
  standalone: true,
})
export class DashboardBookingStatusPipe implements PipeTransform {
  transform(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }
}
