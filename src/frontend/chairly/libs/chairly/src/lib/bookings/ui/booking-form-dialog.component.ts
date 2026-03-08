import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  OutputEmitterRef,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { Booking, CreateBookingRequest } from '../models';

export interface BookingFormSaveEvent {
  id: string | null;
  request: CreateBookingRequest;
}

@Component({
  selector: 'chairly-booking-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './booking-form-dialog.component.html',
})
export class BookingFormDialogComponent {
  readonly booking = input<Booking | null>(null);

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
    serviceIds: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    notes: new FormControl<string | null>(null, {
      validators: [Validators.maxLength(1000)],
    }),
  });

  open(bookingToEdit?: Booking | null): void {
    const b = bookingToEdit !== undefined ? bookingToEdit : this.booking();
    if (b) {
      const startLocal = b.startTime ? new Date(b.startTime).toISOString().slice(0, 16) : '';
      this.form.reset({
        clientId: b.clientId,
        staffMemberId: b.staffMemberId,
        startTime: startLocal,
        serviceIds: b.services.map((s) => s.serviceId).join(', '),
        notes: b.notes,
      });
    } else {
      this.form.reset({
        clientId: '',
        staffMemberId: '',
        startTime: '',
        serviceIds: '',
        notes: null,
      });
    }
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  protected onSave(): void {
    if (this.form.invalid) {
      return;
    }
    const { clientId, staffMemberId, startTime, serviceIds, notes } = this.form.getRawValue();

    const parsedServiceIds = serviceIds
      .split(',')
      .map((id) => id.trim())
      .filter((id) => id.length > 0);

    if (parsedServiceIds.length === 0) {
      return;
    }

    const b = this.booking();
    this.close();
    this.saved.emit({
      id: b?.id ?? null,
      request: {
        clientId,
        staffMemberId,
        startTime: new Date(startTime).toISOString(),
        serviceIds: parsedServiceIds,
        notes: notes || null,
      },
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
