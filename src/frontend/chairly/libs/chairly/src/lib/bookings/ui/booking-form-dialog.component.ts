import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
  OutputEmitterRef,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { DropdownOption, SearchableDropdownComponent } from '@org/shared-lib';

import {
  Booking,
  ClientOption,
  CreateBookingRequest,
  ServiceOption,
  StaffMemberOption,
} from '../models';
import { SetHasPipe } from '../pipes';

export interface BookingFormSaveEvent {
  id: string | null;
  request: CreateBookingRequest;
}

@Component({
  selector: 'chairly-booking-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SearchableDropdownComponent, SetHasPipe],
  templateUrl: './booking-form-dialog.component.html',
})
export class BookingFormDialogComponent {
  readonly booking = input<Booking | null>(null);
  readonly clients = input<ClientOption[]>([]);
  readonly staffMembers = input<StaffMemberOption[]>([]);
  readonly services = input<ServiceOption[]>([]);

  protected readonly clientOptions = computed<DropdownOption[]>(() =>
    this.clients().map((c) => ({ id: c.id, label: `${c.firstName} ${c.lastName}` })),
  );

  protected readonly staffMemberOptions = computed<DropdownOption[]>(() =>
    this.staffMembers().map((m) => ({ id: m.id, label: `${m.firstName} ${m.lastName}` })),
  );

  readonly saved: OutputEmitterRef<BookingFormSaveEvent> = output<BookingFormSaveEvent>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly form = new FormGroup({
    clientId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    staffMemberId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    startTime: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    notes: new FormControl<string | null>(null, {
      validators: [Validators.maxLength(1000)],
    }),
  });

  protected readonly selectedServiceIds = signal<ReadonlySet<string>>(new Set<string>());

  open(bookingToEdit?: Booking | null): void {
    const b = bookingToEdit !== undefined ? bookingToEdit : this.booking();
    if (b) {
      const startLocal = b.startTime ? this.toLocalDateTimeString(new Date(b.startTime)) : '';
      this.form.reset({
        clientId: b.clientId,
        staffMemberId: b.staffMemberId,
        startTime: startLocal,
        notes: b.notes,
      });
      this.selectedServiceIds.set(new Set(b.services.map((s) => s.serviceId)));
    } else {
      this.form.reset({
        clientId: '',
        staffMemberId: '',
        startTime: '',
        notes: null,
      });
      this.selectedServiceIds.set(new Set<string>());
    }
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  protected onServiceToggle(serviceId: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const current = new Set(this.selectedServiceIds());
    if (checked) {
      current.add(serviceId);
    } else {
      current.delete(serviceId);
    }
    this.selectedServiceIds.set(current);
  }

  protected onSave(): void {
    if (this.form.invalid) {
      return;
    }

    const serviceIds = Array.from(this.selectedServiceIds());
    if (serviceIds.length === 0) {
      return;
    }

    const { clientId, staffMemberId, startTime, notes } = this.form.getRawValue();

    const b = this.booking();
    this.close();
    this.saved.emit({
      id: b?.id ?? null,
      request: {
        clientId,
        staffMemberId,
        startTime: new Date(startTime).toISOString(),
        serviceIds,
        notes: notes || null,
      },
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }

  /** Format a Date as YYYY-MM-DDTHH:mm using local timezone (for datetime-local input). */
  private toLocalDateTimeString(date: Date): string {
    const y = date.getFullYear();
    const mo = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const h = String(date.getHours()).padStart(2, '0');
    const mi = String(date.getMinutes()).padStart(2, '0');
    return `${y}-${mo}-${d}T${h}:${mi}`;
  }
}
