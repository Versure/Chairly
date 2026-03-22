import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { UpdateSubscriptionPlanPayload } from '../../models';

@Component({
  selector: 'chairly-admin-update-plan-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './update-plan-dialog.component.html',
  styleUrl: './update-plan-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdatePlanDialogComponent {
  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly currentPlan = input.required<string>();
  readonly currentBillingCycle = input.required<string | null>();
  readonly isSubmitting = input<boolean>(false);

  readonly confirm = output<UpdateSubscriptionPlanPayload>();
  readonly cancelled = output<void>();

  protected readonly form = new FormGroup({
    plan: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    billingCycle: new FormControl<string | null>(null),
  });

  open(): void {
    this.form.patchValue({
      plan: this.currentPlan(),
      billingCycle: this.currentBillingCycle(),
    });
    this.dialogRef().nativeElement.showModal();
    this.document.body.style.overflow = 'hidden';
  }

  close(): void {
    this.dialogRef().nativeElement.close();
    this.form.reset();
    this.document.body.style.overflow = '';
  }

  protected onConfirm(): void {
    if (this.form.valid) {
      this.confirm.emit({
        plan: this.form.value.plan ?? '',
        billingCycle: this.form.value.billingCycle ?? null,
      });
    }
  }

  protected onCancel(): void {
    this.cancelled.emit();
    this.close();
  }
}
