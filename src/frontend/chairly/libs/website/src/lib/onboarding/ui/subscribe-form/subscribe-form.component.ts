import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreateSubscriptionPayload, SubscriptionPlanInfo } from '../../models';

@Component({
  selector: 'chairly-web-subscribe-form',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './subscribe-form.component.html',
  styleUrl: './subscribe-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscribeFormComponent {
  readonly selectedPlan = input.required<SubscriptionPlanInfo>();
  readonly billingCycle = input.required<'monthly' | 'annual'>();
  readonly isTrial = input.required<boolean>();
  readonly isSubmitting = input<boolean>(false);
  readonly submitError = input<string | null>(null);
  readonly formSubmit: OutputEmitterRef<CreateSubscriptionPayload> =
    output<CreateSubscriptionPayload>();

  protected readonly form = new FormGroup({
    salonName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    ownerFirstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    ownerLastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(256)],
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(50)],
    }),
  });

  private resolveBillingCycle(): string | null {
    if (this.isTrial()) {
      return null;
    }
    return this.billingCycle() === 'annual' ? 'Annual' : 'Monthly';
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    const payload: CreateSubscriptionPayload = {
      salonName: formValue.salonName,
      ownerFirstName: formValue.ownerFirstName,
      ownerLastName: formValue.ownerLastName,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber || null,
      plan: this.selectedPlan().slug,
      billingCycle: this.resolveBillingCycle(),
      isTrial: this.isTrial(),
    };

    this.formSubmit.emit(payload);
  }
}
