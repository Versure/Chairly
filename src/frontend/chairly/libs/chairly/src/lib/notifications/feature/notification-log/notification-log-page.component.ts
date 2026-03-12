import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { interval } from 'rxjs';

import { PageHeaderComponent } from '@org/shared-lib';

import { NotificationStore } from '../../data-access';
import { NotificationSummary } from '../../models';
import { NotificationTypeLabelPipe } from '../../pipes';

@Component({
  selector: 'chairly-notification-log-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NotificationTypeLabelPipe, PageHeaderComponent],
  templateUrl: './notification-log-page.component.html',
})
export class NotificationLogPageComponent implements OnInit {
  private readonly notificationStore = inject(NotificationStore);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly notifications = computed<NotificationSummary[]>(() =>
    this.notificationStore.notifications(),
  );
  protected readonly isLoading = computed<boolean>(() => this.notificationStore.isLoading());

  ngOnInit(): void {
    this.notificationStore.loadNotifications();

    interval(30_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.notificationStore.loadNotifications();
      });
  }
}
