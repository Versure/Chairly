import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubscriptionPlanInfo } from '../../models';
import { SubscribeFormComponent } from './subscribe-form.component';

describe('SubscribeFormComponent', () => {
  let component: SubscribeFormComponent;
  let componentRef: ComponentRef<SubscribeFormComponent>;
  let fixture: ComponentFixture<SubscribeFormComponent>;

  const teamPlan: SubscriptionPlanInfo = {
    slug: 'team',
    name: 'Team',
    maxStaff: 5,
    monthlyPrice: 59.99,
    annualPricePerMonth: 53.99,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubscribeFormComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SubscribeFormComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('selectedPlan', teamPlan);
    componentRef.setInput('billingCycle', 'monthly');
    componentRef.setInput('isTrial', false);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display plan summary', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Team');
  });

  it('should display trial text when isTrial is true', () => {
    componentRef.setInput('isTrial', true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('30 dagen gratis');
    expect(compiled.textContent).toContain('Proefperiode starten');
  });

  it('should display paid button text when not trial', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Abonnement aanmaken');
  });

  it('should show validation errors when form is submitted empty', () => {
    const submitButton = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLElement;
    submitButton.click();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Salonnaam is verplicht.');
    expect(compiled.textContent).toContain('Voornaam is verplicht.');
    expect(compiled.textContent).toContain('Achternaam is verplicht.');
    expect(compiled.textContent).toContain('E-mailadres is verplicht.');
  });
});
