import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'chairly-dashboard-stats',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe],
  templateUrl: './dashboard-stats.component.html',
})
export class DashboardStatsComponent {
  readonly todaysBookingsCount = input.required<number>();
  readonly newClientsThisWeek = input.required<number>();
  readonly revenueThisWeek = input.required<number | null>();
  readonly revenueThisMonth = input.required<number | null>();
  readonly isOwner = input.required<boolean>();
  readonly isManager = input.required<boolean>();
}
