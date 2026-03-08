import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';

import { BookingStore } from '../../data-access';
import { Booking, BookingFilter, CreateBookingRequest, UpdateBookingRequest } from '../../models';
import {
  BookingFormDialogComponent,
  BookingFormSaveEvent,
  BookingStatusAction,
  BookingTableComponent,
} from '../../ui';

@Component({
  selector: 'chairly-booking-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BookingFormDialogComponent, BookingTableComponent],
  templateUrl: './booking-list-page.component.html',
})
export class BookingListPageComponent implements OnInit {
  private readonly bookingStore = inject(BookingStore);

  private readonly formDialogRef = viewChild.required(BookingFormDialogComponent);

  protected readonly editingBooking = signal<Booking | null>(null);

  protected readonly bookings = computed<Booking[]>(() => this.bookingStore.bookings());
  protected readonly isLoading = computed<boolean>(() => this.bookingStore.loading());

  protected readonly filterDate = signal('');
  protected readonly filterStaffMemberId = signal('');

  ngOnInit(): void {
    this.bookingStore.loadBookings();
  }

  protected onFilter(): void {
    const filter: BookingFilter = {};
    const date = this.filterDate();
    const staffId = this.filterStaffMemberId();
    if (date) {
      filter.date = date;
    }
    if (staffId) {
      filter.staffMemberId = staffId;
    }
    this.bookingStore.loadBookings(filter);
  }

  protected onAddBooking(): void {
    this.editingBooking.set(null);
    this.formDialogRef().open(null);
  }

  protected onBookingSelected(booking: Booking): void {
    this.editingBooking.set(booking);
    this.formDialogRef().open(booking);
  }

  protected onStatusAction(event: BookingStatusAction): void {
    switch (event.action) {
      case 'confirm':
        this.bookingStore.confirmBooking(event.bookingId);
        break;
      case 'start':
        this.bookingStore.startBooking(event.bookingId);
        break;
      case 'complete':
        this.bookingStore.completeBooking(event.bookingId);
        break;
      case 'cancel':
        this.bookingStore.cancelBooking(event.bookingId);
        break;
      case 'noShow':
        this.bookingStore.markNoShow(event.bookingId);
        break;
    }
  }

  protected onFormSaved(event: BookingFormSaveEvent): void {
    if (event.id) {
      this.bookingStore.updateBooking(event.id, event.request as UpdateBookingRequest);
    } else {
      this.bookingStore.createBooking(event.request as CreateBookingRequest);
    }
    this.editingBooking.set(null);
  }

  protected onFormCancelled(): void {
    this.editingBooking.set(null);
  }

  protected onDateChange(event: Event): void {
    this.filterDate.set((event.target as HTMLInputElement).value);
  }

  protected onStaffIdChange(event: Event): void {
    this.filterStaffMemberId.set((event.target as HTMLInputElement).value);
  }
}
