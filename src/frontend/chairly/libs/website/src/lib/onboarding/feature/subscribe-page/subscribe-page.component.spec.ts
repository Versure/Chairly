import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';

import { of } from 'rxjs';

import { SubscribePageComponent } from './subscribe-page.component';

describe('SubscribePageComponent', () => {
  let component: SubscribePageComponent;
  let fixture: ComponentFixture<SubscribePageComponent>;
  let httpMock: HttpTestingController;

  const plans = [
    {
      slug: 'starter',
      name: 'Starter',
      maxStaff: 1,
      monthlyPrice: 14.99,
      annualPricePerMonth: 13.49,
    },
    { slug: 'team', name: 'Team', maxStaff: 5, monthlyPrice: 59.99, annualPricePerMonth: 53.99 },
    { slug: 'salon', name: 'Salon', maxStaff: 15, monthlyPrice: 149.0, annualPricePerMonth: 134.1 },
  ];

  function createComponent(queryParams: Record<string, string>): void {
    TestBed.configureTestingModule({
      imports: [SubscribePageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: of({
              get: (key: string) => queryParams[key] ?? null,
            }),
          },
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(SubscribePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('should create for trial flow', () => {
    createComponent({ plan: 'starter', trial: 'true' });
    httpMock.expectOne('/api/onboarding/plans').flush(plans);
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display trial heading', () => {
    createComponent({ plan: 'starter', trial: 'true' });
    httpMock.expectOne('/api/onboarding/plans').flush(plans);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Start uw gratis proefperiode');
  });

  it('should display paid heading', () => {
    createComponent({ plan: 'team', trial: 'false', cyclus: 'monthly' });
    httpMock.expectOne('/api/onboarding/plans').flush(plans);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Uw gegevens');
  });

  it('should render subscribe form', () => {
    createComponent({ plan: 'team', trial: 'false', cyclus: 'monthly' });
    httpMock.expectOne('/api/onboarding/plans').flush(plans);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const form = compiled.querySelector('chairly-web-subscribe-form');
    expect(form).toBeTruthy();
  });
});
