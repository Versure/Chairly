import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';

import { AuthStore, PageHeaderComponent } from '@org/shared-lib';

import { DashboardStore } from '../../data-access';
import { DashboardBookingListComponent, DashboardStatsComponent } from '../../ui';

@Component({
  selector: 'chairly-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DashboardStatsComponent, DashboardBookingListComponent, PageHeaderComponent],
  templateUrl: './dashboard-page.component.html',
})
export class DashboardPageComponent implements OnInit {
  protected readonly store = inject(DashboardStore);
  protected readonly authStore = inject(AuthStore);

  ngOnInit(): void {
    this.store.loadDashboard();
  }
}
