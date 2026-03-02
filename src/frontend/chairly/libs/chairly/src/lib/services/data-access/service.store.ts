import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import {
  CreateServiceRequest,
  ServiceResponse,
  UpdateServiceRequest,
} from '../models';
import { ServiceApiService } from './service-api.service';

export interface ServiceState {
  services: ServiceResponse[];
  isLoading: boolean;
  error: string | null;
}

const initialState: ServiceState = {
  services: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

function replaceService(
  services: ServiceResponse[],
  id: string,
  updated: ServiceResponse
): ServiceResponse[] {
  return services.map((s) => (s.id === id ? updated : s));
}

function removeService(
  services: ServiceResponse[],
  id: string
): ServiceResponse[] {
  return services.filter((s) => s.id !== id);
}

function groupByCategory(
  services: ServiceResponse[]
): Record<string, ServiceResponse[]> {
  const result: Record<string, ServiceResponse[]> = {};
  for (const service of services) {
    const key = service.categoryId ?? 'uncategorized';
    if (!result[key]) {
      result[key] = [];
    }
    result[key].push(service);
  }
  return result;
}

export const ServiceStore = signalStore(
  withState<ServiceState>(initialState),
  withComputed((store) => ({
    activeServices: computed(() => store.services().filter((s) => s.isActive)),
    servicesByCategory: computed(() => groupByCategory(store.services())),
  })),
  withMethods((store) => {
    const serviceApi = inject(ServiceApiService);

    return {
      loadServices(): void {
        patchState(store, { isLoading: true, error: null });
        serviceApi
          .getAll()
          .pipe(take(1))
          .subscribe({
            next: (services) =>
              patchState(store, { services, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      createService(request: CreateServiceRequest): void {
        serviceApi
          .create(request)
          .pipe(take(1))
          .subscribe({
            next: (service) =>
              patchState(store, (state) => ({
                services: [...state.services, service],
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      updateService(id: string, request: UpdateServiceRequest): void {
        serviceApi
          .update(id, request)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                services: replaceService(state.services, id, updated),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      deleteService(id: string): void {
        serviceApi
          .delete(id)
          .pipe(take(1))
          .subscribe({
            next: () =>
              patchState(store, (state) => ({
                services: removeService(state.services, id),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      toggleActive(id: string): void {
        serviceApi
          .toggleActive(id)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                services: replaceService(state.services, id, updated),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },
    };
  })
);
