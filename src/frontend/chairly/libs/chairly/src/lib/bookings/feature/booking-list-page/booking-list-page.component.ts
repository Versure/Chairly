import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { take } from 'rxjs';

import { InvoiceGenerationService } from '@org/shared-lib';

import { BookingStore } from '../../data-access';
import {
  Booking,
  BookingFilter,
  ClientOption,
  CreateBookingRequest,
  ServiceOption,
  StaffMemberOption,
  UpdateBookingRequest,
} from '../../models';
import {
  BookingFormDialogComponent,
  BookingFormSaveEvent,
  BookingScheduleComponent,
  BookingStatusAction,
  BookingTableComponent,
} from '../../ui';

@Component({
  selector: 'chairly-booking-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BookingFormDialogComponent, BookingScheduleComponent, BookingTableComponent],
  templateUrl: './booking-list-page.component.html',
})
export class BookingListPageComponent implements OnInit {
  private readonly bookingStore = inject(BookingStore);
  private readonly invoiceGenerationService = inject(InvoiceGenerationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly formDialogRef = viewChild.required(BookingFormDialogComponent);

  protected readonly invoiceMessage = signal<string | null>(null);
  protected readonly invoiceId = signal<string | null>(null);
  protected readonly isGeneratingInvoice = signal(false);

  protected readonly viewMode = signal<'list' | 'schedule'>('list');
  protected readonly editingBooking = signal<Booking | null>(null);

  protected readonly bookings = computed<Booking[]>(() => this.bookingStore.bookings());
  protected readonly isLoading = computed<boolean>(() => this.bookingStore.loading());
  protected readonly clients = computed<ClientOption[]>(() => this.bookingStore.clients());
  protected readonly staffMembers = computed<StaffMemberOption[]>(() =>
    this.bookingStore.staffMembers(),
  );
  protected readonly services = computed<ServiceOption[]>(() => this.bookingStore.services());
  protected readonly clientNameMap = computed<Record<string, string>>(() =>
    this.bookingStore.clientNameMap(),
  );
  protected readonly staffMemberNameMap = computed<Record<string, string>>(() =>
    this.bookingStore.staffMemberNameMap(),
  );

  protected readonly filterDate = signal(new Date().toISOString().split('T')[0]);
  protected readonly filterStaffMemberId = signal('');

  ngOnInit(): void {
    this.bookingStore.loadBookings({ date: this.filterDate() });
    this.bookingStore.loadReferenceData();
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
      case 'generateInvoice':
        this.onGenerateInvoice(event.bookingId);
        break;
    }
  }

  protected onGenerateInvoice(bookingId: string): void {
    this.isGeneratingInvoice.set(true);
    this.invoiceMessage.set(null);
    this.invoiceId.set(null);

    this.invoiceGenerationService
      .generateInvoice(bookingId)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invoice) => {
          this.isGeneratingInvoice.set(false);
          this.invoiceMessage.set('Factuur succesvol aangemaakt');
          this.invoiceId.set(invoice.id);
        },
        error: (err: unknown) => {
          this.isGeneratingInvoice.set(false);
          const httpErr = err as { status?: number };
          if (httpErr.status === 409) {
            this.invoiceMessage.set('Er bestaat al een factuur voor deze boeking');
          } else {
            this.invoiceMessage.set('Fout bij het genereren van de factuur');
          }
        },
      });
  }

  protected onViewInvoice(): void {
    const id = this.invoiceId();
    if (id) {
      this.router.navigate(['/facturen', id]);
    }
  }

  protected dismissInvoiceMessage(): void {
    this.invoiceMessage.set(null);
    this.invoiceId.set(null);
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

  protected onStaffFilterChange(event: Event): void {
    this.filterStaffMemberId.set((event.target as HTMLSelectElement).value);
  }

  protected setViewMode(mode: 'list' | 'schedule'): void {
    this.viewMode.set(mode);
  }
}
