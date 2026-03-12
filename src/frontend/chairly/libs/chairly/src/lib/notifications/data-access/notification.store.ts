import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { NotificationSummary } from '../models';
import { NotificationsApiService } from './notifications-api.service';

export interface NotificationState {
  notifications: NotificationSummary[];
  isLoading: boolean;
  error: string | null;
}

const initialState: NotificationState = {
  notifications: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const NotificationStore = signalStore(
  withState<NotificationState>(initialState),
  withMethods((store) => {
    const notificationsApi = inject(NotificationsApiService);

    return {
      loadNotifications(): void {
        patchState(store, { isLoading: true, error: null });
        notificationsApi
          .getNotifications()
          .pipe(take(1))
          .subscribe({
            next: (notifications) => patchState(store, { notifications, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoading: false }),
          });
      },
    };
  }),
);
