import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PricingPageComponent } from './pricing-page.component';

describe('PricingPageComponent', () => {
  let component: PricingPageComponent;
  let fixture: ComponentFixture<PricingPageComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PricingPageComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PricingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    httpMock.expectOne('/api/onboarding/plans').flush([]);
    expect(component).toBeTruthy();
  });

  it('should display heading', () => {
    httpMock.expectOne('/api/onboarding/plans').flush([]);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Kies het plan dat bij uw salon past');
  });

  it('should display plan cards after loading', () => {
    const plans = [
      {
        slug: 'starter',
        name: 'Starter',
        maxStaff: 1,
        monthlyPrice: 14.99,
        annualPricePerMonth: 13.49,
      },
      { slug: 'team', name: 'Team', maxStaff: 5, monthlyPrice: 59.99, annualPricePerMonth: 53.99 },
      {
        slug: 'salon',
        name: 'Salon',
        maxStaff: 15,
        monthlyPrice: 149.0,
        annualPricePerMonth: 134.1,
      },
    ];
    httpMock.expectOne('/api/onboarding/plans').flush(plans);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('chairly-web-pricing-card');
    // 1 trial + 3 paid = 4 cards
    expect(cards.length).toBe(4);
  });

  it('should display feature comparison table', () => {
    httpMock.expectOne('/api/onboarding/plans').flush([]);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Vergelijk plannen');
    expect(compiled.textContent).toContain('Boekingen beheren');
    expect(compiled.textContent).toContain('Aantal medewerkers');
  });

  it('should display FAQ section', () => {
    httpMock.expectOne('/api/onboarding/plans').flush([]);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Veelgestelde vragen');
    expect(compiled.textContent).toContain('Hoe werkt de gratis proefperiode?');
  });
});
