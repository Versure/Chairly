import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { switchMap, take } from 'rxjs';

import {
  AdminSubscriptionDetail,
  AdminSubscriptionListItem,
  CancelSubscriptionPayload,
  SubscriptionListFilters,
  UpdateSubscriptionPlanPayload,
} from '../models';
import { AdminSubscriptionApiService } from './admin-subscription-api.service';

export interface AdminSubscriptionState {
  items: AdminSubscriptionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  selectedSubscription: AdminSubscriptionDetail | null;
  isDetailLoading: boolean;
  error: string | null;
}

const initialState: AdminSubscriptionState = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 25,
  isLoading: false,
  selectedSubscription: null,
  isDetailLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const AdminSubscriptionStore = signalStore(
  withState<AdminSubscriptionState>(initialState),
  withComputed((store) => ({
    totalPages: computed(() => Math.max(1, Math.ceil(store.totalCount() / store.pageSize()))),
  })),
  withMethods((store) => {
    const api = inject(AdminSubscriptionApiService);

    return {
      loadSubscriptions(filters: SubscriptionListFilters): void {
        patchState(store, { isLoading: true, error: null });
        api
          .getSubscriptions(filters)
          .pipe(take(1))
          .subscribe({
            next: (response) =>
              patchState(store, {
                items: response.items,
                totalCount: response.totalCount,
                page: response.page,
                pageSize: response.pageSize,
                isLoading: false,
              }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoading: false }),
          });
      },

      loadSubscription(id: string): void {
        patchState(store, { isDetailLoading: true, error: null, selectedSubscription: null });
        api
          .getSubscription(id)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, { selectedSubscription: detail, isDetailLoading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isDetailLoading: false }),
          });
      },

      provisionSubscription(id: string, currentFilters: SubscriptionListFilters): void {
        patchState(store, { isDetailLoading: true, error: null });
        api
          .provisionSubscription(id)
          .pipe(
            switchMap((detail) => {
              patchState(store, { selectedSubscription: detail, isDetailLoading: false });
              return api.getSubscriptions(currentFilters);
            }),
            take(1),
          )
          .subscribe({
            next: (response) =>
              patchState(store, {
                items: response.items,
                totalCount: response.totalCount,
              }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isDetailLoading: false }),
          });
      },

      cancelSubscription(
        id: string,
        payload: CancelSubscriptionPayload,
        currentFilters: SubscriptionListFilters,
      ): void {
        patchState(store, { isDetailLoading: true, error: null });
        api
          .cancelSubscription(id, payload)
          .pipe(
            switchMap((detail) => {
              patchState(store, { selectedSubscription: detail, isDetailLoading: false });
              return api.getSubscriptions(currentFilters);
            }),
            take(1),
          )
          .subscribe({
            next: (response) =>
              patchState(store, {
                items: response.items,
                totalCount: response.totalCount,
              }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isDetailLoading: false }),
          });
      },

      updateSubscriptionPlan(
        id: string,
        payload: UpdateSubscriptionPlanPayload,
        currentFilters: SubscriptionListFilters,
      ): void {
        patchState(store, { isDetailLoading: true, error: null });
        api
          .updateSubscriptionPlan(id, payload)
          .pipe(
            switchMap((detail) => {
              patchState(store, { selectedSubscription: detail, isDetailLoading: false });
              return api.getSubscriptions(currentFilters);
            }),
            take(1),
          )
          .subscribe({
            next: (response) =>
              patchState(store, {
                items: response.items,
                totalCount: response.totalCount,
              }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isDetailLoading: false }),
          });
      },
    };
  }),
);
