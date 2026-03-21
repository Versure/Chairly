import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { Router } from '@angular/router';

import { OnboardingApiService } from '../../data-access';
import { SubscriptionPlanInfo } from '../../models';
import { FooterComponent, HeaderComponent, PricingCardComponent } from '../../ui';

@Component({
  selector: 'chairly-web-pricing-page',
  standalone: true,
  imports: [HeaderComponent, FooterComponent, PricingCardComponent],
  templateUrl: './pricing-page.component.html',
  styleUrl: './pricing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricingPageComponent implements OnInit {
  private readonly onboardingApi = inject(OnboardingApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  protected readonly plans = signal<SubscriptionPlanInfo[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly billingCycle = signal<'monthly' | 'annual'>('monthly');

  // Default starter plan for the trial card when plans are still loading
  protected readonly trialPlan: SubscriptionPlanInfo = {
    slug: 'starter',
    name: 'Starter',
    maxStaff: 1,
    monthlyPrice: 14.99,
    annualPricePerMonth: 13.49,
  };

  ngOnInit(): void {
    this.title.setTitle('Prijzen - Chairly | Salon software abonnementen');
    this.meta.updateTag({
      name: 'description',
      content:
        'Bekijk de abonnementsprijzen van Chairly. Kies uit Starter, Team of Salon. Alle plannen bevatten boekingen, klantenbeheer, facturatie en meldingen. 30 dagen gratis proberen.',
    });
    this.meta.updateTag({
      property: 'og:title',
      content: 'Prijzen - Chairly',
    });
    this.meta.updateTag({
      property: 'og:description',
      content:
        'Eenvoudige, transparante prijzen voor salon software. Vanaf EUR 14,99 per maand. Probeer 30 dagen gratis.',
    });

    this.onboardingApi
      .getSubscriptionPlans()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (plans) => {
          this.plans.set(plans);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }

  protected toggleBillingCycle(cycle: 'monthly' | 'annual'): void {
    this.billingCycle.set(cycle);
  }

  protected onSelectTrial(): void {
    void this.router.navigate(['/abonneren'], {
      queryParams: { plan: 'starter', trial: 'true' },
    });
  }

  protected onSelectPlan(plan: SubscriptionPlanInfo): void {
    void this.router.navigate(['/abonneren'], {
      queryParams: {
        plan: plan.slug,
        trial: 'false',
        cyclus: this.billingCycle(),
      },
    });
  }
}
