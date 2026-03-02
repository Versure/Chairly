import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import {
  CreateServiceCategoryRequest,
  ServiceCategoryResponse,
  UpdateServiceCategoryRequest,
} from '../util';
import { ServiceCategoryApiService } from './service-category-api.service';

export interface ServiceCategoryState {
  categories: ServiceCategoryResponse[];
  isLoading: boolean;
  error: string | null;
}

const initialState: ServiceCategoryState = {
  categories: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

function replaceCategory(
  categories: ServiceCategoryResponse[],
  id: string,
  updated: ServiceCategoryResponse
): ServiceCategoryResponse[] {
  return categories.map((c) => (c.id === id ? updated : c));
}

function removeCategory(
  categories: ServiceCategoryResponse[],
  id: string
): ServiceCategoryResponse[] {
  return categories.filter((c) => c.id !== id);
}

export const ServiceCategoryStore = signalStore(
  withState<ServiceCategoryState>(initialState),
  withMethods((store) => {
    const categoryService = inject(ServiceCategoryApiService);

    return {
      loadCategories(): void {
        patchState(store, { isLoading: true, error: null });
        categoryService
          .getAll()
          .pipe(take(1))
          .subscribe({
            next: (categories) =>
              patchState(store, { categories, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      createCategory(request: CreateServiceCategoryRequest): void {
        categoryService
          .create(request)
          .pipe(take(1))
          .subscribe({
            next: (category) =>
              patchState(store, (state) => ({
                categories: [...state.categories, category],
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      updateCategory(id: string, request: UpdateServiceCategoryRequest): void {
        categoryService
          .update(id, request)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                categories: replaceCategory(state.categories, id, updated),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      deleteCategory(id: string): void {
        categoryService
          .delete(id)
          .pipe(take(1))
          .subscribe({
            next: () =>
              patchState(store, (state) => ({
                categories: removeCategory(state.categories, id),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },
    };
  })
);
