import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  output,
  OutputEmitterRef,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { AddLineItemRequest } from '../../models';

export type LineItemDialogMode = 'surcharge' | 'discount';

@Component({
  selector: 'chairly-line-item-form-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './line-item-form-dialog.component.html',
})
export class LineItemFormDialogComponent {
  readonly saved: OutputEmitterRef<AddLineItemRequest> = output<AddLineItemRequest>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected mode: LineItemDialogMode = 'surcharge';

  protected readonly form = new FormGroup({
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    amount: new FormControl<number>(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    vatPercentage: new FormControl<number>(21, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0), Validators.max(100)],
    }),
  });

  protected get dialogTitle(): string {
    return this.mode === 'surcharge' ? 'Toeslag toevoegen' : 'Korting toevoegen';
  }

  open(mode: LineItemDialogMode): void {
    this.mode = mode;
    this.form.reset({
      description: '',
      amount: 0,
      vatPercentage: 21,
    });
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
    const { description, amount, vatPercentage } = this.form.getRawValue();
    const unitPrice = this.mode === 'discount' ? -Math.abs(amount) : Math.abs(amount);
    this.close();
    this.saved.emit({
      description,
      quantity: 1,
      unitPrice,
      vatPercentage,
      isManual: true,
    });
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
