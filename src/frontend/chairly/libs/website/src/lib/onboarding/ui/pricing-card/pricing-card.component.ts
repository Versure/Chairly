import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  OutputEmitterRef,
} from '@angular/core';

import { SubscriptionPlanInfo } from '../../models';

@Component({
  selector: 'chairly-web-pricing-card',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './pricing-card.component.html',
  styleUrl: './pricing-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricingCardComponent {
  readonly plan = input.required<SubscriptionPlanInfo>();
  readonly billingCycle = input.required<'monthly' | 'annual'>();
  readonly highlighted = input<boolean>(false);
  readonly isTrial = input<boolean>(false);
  readonly selectPlan: OutputEmitterRef<void> = output<void>();

  protected readonly displayPrice = computed<number>(() => {
    if (this.isTrial()) {
      return 0;
    }
    return this.billingCycle() === 'annual'
      ? this.plan().annualPricePerMonth
      : this.plan().monthlyPrice;
  });

  protected readonly staffDescription = computed<string>(() => {
    const max = this.plan().maxStaff;
    return max === 1 ? '1 medewerker' : `Tot ${max} medewerkers`;
  });
}
