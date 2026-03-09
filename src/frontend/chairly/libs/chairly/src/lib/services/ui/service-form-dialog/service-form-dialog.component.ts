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

import {
  CreateServiceRequest,
  ServiceCategoryResponse,
  ServiceResponse,
  UpdateServiceRequest,
} from '../../models';
import { formatDurationToTimeSpan, parseDuration } from '../../util';

@Component({
  selector: 'chairly-service-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './service-form-dialog.component.html',
})
export class ServiceFormDialogComponent {
  readonly categories = input.required<ServiceCategoryResponse[]>();
  readonly service = input<ServiceResponse | null>(null);

  readonly saved: OutputEmitterRef<CreateServiceRequest | UpdateServiceRequest> = output<
    CreateServiceRequest | UpdateServiceRequest
  >();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(150)],
    }),
    description: new FormControl<string | null>(null, {
      validators: [Validators.maxLength(2000)],
    }),
    duration: new FormControl<number>(30, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    price: new FormControl<number>(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    categoryId: new FormControl<string | null>(null),
  });

  open(serviceToEdit?: ServiceResponse | null): void {
    const svc = serviceToEdit !== undefined ? serviceToEdit : this.service();
    if (svc) {
      this.form.reset({
        name: svc.name,
        description: svc.description,
        duration: parseDuration(svc.duration),
        price: svc.price,
        categoryId: svc.categoryId,
      });
    } else {
      this.form.reset({
        name: '',
        description: null,
        duration: 30,
        price: 0,
        categoryId: null,
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
    const { name, description, duration, price, categoryId } = this.form.getRawValue();
    this.close();
    this.saved.emit({
      name,
      description,
      duration: formatDurationToTimeSpan(duration),
      price,
      categoryId,
      sortOrder: 0,
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
