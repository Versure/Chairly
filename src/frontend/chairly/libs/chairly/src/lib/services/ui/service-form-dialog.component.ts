import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
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
} from '../models';
import { formatDurationToTimeSpan, parseDuration } from '../util';

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

  readonly saved: OutputEmitterRef<CreateServiceRequest | UpdateServiceRequest> =
    output<CreateServiceRequest | UpdateServiceRequest>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

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
    sortOrder: new FormControl<number>(0, { nonNullable: true }),
  });

  open(): void {
    const svc = this.service();
    if (svc) {
      this.form.reset({
        name: svc.name,
        description: svc.description,
        duration: parseDuration(svc.duration),
        price: svc.price,
        categoryId: svc.categoryId,
        sortOrder: svc.sortOrder,
      });
    } else {
      this.form.reset({
        name: '',
        description: null,
        duration: 30,
        price: 0,
        categoryId: null,
        sortOrder: 0,
      });
    }
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.dialogRef().nativeElement.close();
  }

  protected onSave(): void {
    if (this.form.invalid) {
      return;
    }
    const { name, description, duration, price, categoryId, sortOrder } =
      this.form.getRawValue();
    this.close();
    this.saved.emit({
      name,
      description,
      duration: formatDurationToTimeSpan(duration),
      price,
      categoryId,
      sortOrder,
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
