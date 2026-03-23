import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { DashboardResponse } from '../models';
import { DashboardApiService } from './dashboard-api.service';

export interface DashboardState {
  dashboard: DashboardResponse | null;
  loading: boolean;
  error: string | null;
}

const initialState: DashboardState = {
  dashboard: null,
  loading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const DashboardStore = signalStore(
  withState<DashboardState>(initialState),
  withComputed((store) => ({
    isLoaded: computed(() => store.dashboard() !== null),
  })),
  withMethods((store) => {
    const dashboardApi = inject(DashboardApiService);

    return {
      loadDashboard(): void {
        patchState(store, { loading: true, error: null });
        dashboardApi
          .getDashboard()
          .pipe(take(1))
          .subscribe({
            next: (dashboard) => patchState(store, { dashboard, loading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), loading: false }),
          });
      },
    };
  }),
);
