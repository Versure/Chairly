import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { Booking, BookingFilter, CreateBookingRequest, UpdateBookingRequest } from '../models';
import { BookingApiService } from './booking-api.service';

export interface BookingState {
  bookings: Booking[];
  selectedBooking: Booking | null;
  loading: boolean;
  error: string | null;
  activeFilter: BookingFilter;
}

const initialState: BookingState = {
  bookings: [],
  selectedBooking: null,
  loading: false,
  error: null,
  activeFilter: {},
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const BookingStore = signalStore(
  withState<BookingState>(initialState),
  withMethods((store) => {
    const bookingApi = inject(BookingApiService);

    function reloadBookings(): void {
      const filter = store.activeFilter();
      patchState(store, { loading: true, error: null });
      bookingApi
        .getBookings(filter)
        .pipe(take(1))
        .subscribe({
          next: (bookings) => patchState(store, { bookings, loading: false }),
          error: (err: unknown) =>
            patchState(store, { error: toErrorMessage(err), loading: false }),
        });
    }

    return {
      loadBookings(filter?: BookingFilter): void {
        const activeFilter = filter ?? store.activeFilter();
        patchState(store, { activeFilter, loading: true, error: null });
        bookingApi
          .getBookings(activeFilter)
          .pipe(take(1))
          .subscribe({
            next: (bookings) => patchState(store, { bookings, loading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), loading: false }),
          });
      },

      createBooking(request: CreateBookingRequest): void {
        bookingApi
          .createBooking(request)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      updateBooking(id: string, request: UpdateBookingRequest): void {
        bookingApi
          .updateBooking(id, request)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      cancelBooking(id: string): void {
        bookingApi
          .cancelBooking(id)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      confirmBooking(id: string): void {
        bookingApi
          .confirmBooking(id)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      startBooking(id: string): void {
        bookingApi
          .startBooking(id)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      completeBooking(id: string): void {
        bookingApi
          .completeBooking(id)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      markNoShow(id: string): void {
        bookingApi
          .markNoShow(id)
          .pipe(take(1))
          .subscribe({
            next: () => reloadBookings(),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      selectBooking(booking: Booking | null): void {
        patchState(store, { selectedBooking: booking });
      },
    };
  }),
);
