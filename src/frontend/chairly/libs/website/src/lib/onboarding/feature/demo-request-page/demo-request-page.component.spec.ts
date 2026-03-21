import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { of, throwError } from 'rxjs';

import { OnboardingApiService } from '../../data-access';
import { DemoRequestPageComponent } from './demo-request-page.component';

describe('DemoRequestPageComponent', () => {
  let component: DemoRequestPageComponent;
  let fixture: ComponentFixture<DemoRequestPageComponent>;
  let mockApi: { submitDemoRequest: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    mockApi = {
      submitDemoRequest: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [DemoRequestPageComponent],
      providers: [provideRouter([]), { provide: OnboardingApiService, useValue: mockApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(DemoRequestPageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call OnboardingApiService.submitDemoRequest on form submit', () => {
    mockApi.submitDemoRequest.mockReturnValue(
      of({
        id: '123',
        contactName: 'Test',
        salonName: 'Salon',
        email: 'test@test.nl',
        createdAtUtc: '2026-01-01T00:00:00Z',
      }),
    );

    component['form'].patchValue({
      contactName: 'Test',
      salonName: 'Salon',
      email: 'test@test.nl',
    });

    component['onSubmit']();

    expect(mockApi.submitDemoRequest).toHaveBeenCalledWith({
      contactName: 'Test',
      salonName: 'Salon',
      email: 'test@test.nl',
      phoneNumber: null,
      message: null,
    });
  });

  it('should disable submit button while submitting', () => {
    component['isSubmitting'].set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('should navigate to confirmation page on success', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    mockApi.submitDemoRequest.mockReturnValue(
      of({
        id: '123',
        contactName: 'Test',
        salonName: 'Salon',
        email: 'test@test.nl',
        createdAtUtc: '2026-01-01T00:00:00Z',
      }),
    );

    component['form'].patchValue({
      contactName: 'Test',
      salonName: 'Salon',
      email: 'test@test.nl',
    });

    component['onSubmit']();

    expect(navigateSpy).toHaveBeenCalledWith(['/bevestiging'], {
      queryParams: { type: 'demo' },
    });
  });

  it('should display error message on failure', () => {
    mockApi.submitDemoRequest.mockReturnValue(throwError(() => new Error('Server error')));

    component['form'].patchValue({
      contactName: 'Test',
      salonName: 'Salon',
      email: 'test@test.nl',
    });

    component['onSubmit']();
    fixture.detectChanges();

    expect(component['submitError']()).toBe(
      'Er is een fout opgetreden. Probeer het later opnieuw.',
    );
  });
});
