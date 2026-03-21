import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { map } from 'rxjs';

import { OnboardingApiService } from '../../data-access';
import { CreateSubscriptionPayload, SubscriptionPlanInfo } from '../../models';
import { FooterComponent, HeaderComponent, SubscribeFormComponent } from '../../ui';

@Component({
  selector: 'chairly-web-subscribe-page',
  standalone: true,
  imports: [HeaderComponent, FooterComponent, SubscribeFormComponent],
  templateUrl: './subscribe-page.component.html',
  styleUrl: './subscribe-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscribePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly onboardingApi = inject(OnboardingApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly queryParams = toSignal(
    this.route.queryParamMap.pipe(
      map((params) => ({
        plan: params.get('plan'),
        trial: params.get('trial'),
        cyclus: params.get('cyclus'),
      })),
    ),
  );

  protected readonly plans = signal<SubscriptionPlanInfo[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly isTrial = computed<boolean>(() => this.queryParams()?.trial === 'true');

  protected readonly billingCycle = computed<'monthly' | 'annual'>(() => {
    const cyclus = this.queryParams()?.cyclus;
    return cyclus === 'annual' ? 'annual' : 'monthly';
  });

  protected readonly selectedPlan = computed<SubscriptionPlanInfo | null>(() => {
    const planSlug = this.queryParams()?.plan;
    if (!planSlug) {
      return null;
    }
    return this.plans().find((p) => p.slug === planSlug) ?? null;
  });

  protected readonly heading = computed<string>(() =>
    this.isTrial() ? 'Start uw gratis proefperiode' : 'Uw gegevens',
  );

  protected readonly subheading = computed<string>(() => {
    const plan = this.selectedPlan();
    if (this.isTrial()) {
      return 'Vul uw gegevens in om 30 dagen gratis te starten met Chairly.';
    }
    return plan
      ? `Vul uw gegevens in om uw ${plan.name}-abonnement te activeren.`
      : 'Vul uw gegevens in.';
  });

  ngOnInit(): void {
    this.onboardingApi
      .getSubscriptionPlans()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (plans) => {
          this.plans.set(plans);
          this.isLoading.set(false);

          // Redirect if no valid plan param
          const planSlug = this.queryParams()?.plan;
          if (!planSlug || !plans.find((p) => p.slug === planSlug)) {
            void this.router.navigate(['/prijzen']);
          }
        },
        error: () => {
          this.isLoading.set(false);
          void this.router.navigate(['/prijzen']);
        },
      });
  }

  protected onFormSubmit(payload: CreateSubscriptionPayload): void {
    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.onboardingApi
      .createSubscription(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/bevestiging'], {
            queryParams: { type: 'abonnement' },
          });
        },
        error: () => {
          this.isSubmitting.set(false);
          this.submitError.set('Er is een fout opgetreden. Probeer het later opnieuw.');
        },
      });
  }
}
