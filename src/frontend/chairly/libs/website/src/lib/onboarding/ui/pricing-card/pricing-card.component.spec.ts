import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';
import { ComponentRef, DEFAULT_CURRENCY_CODE, LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubscriptionPlanInfo } from '../../models';
import { PricingCardComponent } from './pricing-card.component';

registerLocaleData(localeNl);

describe('PricingCardComponent', () => {
  let component: PricingCardComponent;
  let componentRef: ComponentRef<PricingCardComponent>;
  let fixture: ComponentFixture<PricingCardComponent>;

  const starterPlan: SubscriptionPlanInfo = {
    slug: 'starter',
    name: 'Starter',
    maxStaff: 1,
    monthlyPrice: 14.99,
    annualPricePerMonth: 13.49,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PricingCardComponent],
      providers: [
        { provide: LOCALE_ID, useValue: 'nl-NL' },
        { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PricingCardComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('plan', starterPlan);
    componentRef.setInput('billingCycle', 'monthly');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display plan name', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Starter');
  });

  it('should display monthly price', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('14,99');
  });

  it('should display annual price when billingCycle is annual', () => {
    componentRef.setInput('billingCycle', 'annual');
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('13,49');
  });

  it('should display trial card when isTrial is true', () => {
    componentRef.setInput('isTrial', true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Gratis proberen');
    expect(compiled.textContent).toContain('30 dagen gratis');
    expect(compiled.textContent).toContain('Gratis starten');
  });

  it('should display "Populair" badge when highlighted', () => {
    componentRef.setInput('highlighted', true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Populair');
  });
});
