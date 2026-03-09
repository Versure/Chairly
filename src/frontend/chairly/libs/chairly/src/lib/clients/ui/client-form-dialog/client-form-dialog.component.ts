import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { ClientResponse, CreateClientRequest } from '../../models';

@Component({
  selector: 'chairly-client-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './client-form-dialog.component.html',
})
export class ClientFormDialogComponent {
  readonly client: InputSignal<ClientResponse | null> = input<ClientResponse | null>(null);

  readonly saved: OutputEmitterRef<CreateClientRequest> = output<CreateClientRequest>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly form = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    email: new FormControl<string | null>(null, {
      validators: [Validators.email],
    }),
    phoneNumber: new FormControl<string | null>(null),
    notes: new FormControl<string | null>(null, {
      validators: [Validators.maxLength(1000)],
    }),
  });

  open(client?: ClientResponse | null): void {
    const clientData = client !== undefined ? client : this.client();
    if (clientData) {
      this.form.reset({
        firstName: clientData.firstName,
        lastName: clientData.lastName,
        email: clientData.email,
        phoneNumber: clientData.phoneNumber,
        notes: clientData.notes,
      });
    } else {
      this.form.reset({
        firstName: '',
        lastName: '',
        email: null,
        phoneNumber: null,
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
    const { firstName, lastName, email, phoneNumber, notes } = this.form.getRawValue();
    this.close();
    this.saved.emit({ firstName, lastName, email, phoneNumber, notes });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
