import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { of, throwError } from 'rxjs';

import { OnboardingApiService } from '../../data-access';
import { SignUpPageComponent } from './sign-up-page.component';

describe('SignUpPageComponent', () => {
  let component: SignUpPageComponent;
  let fixture: ComponentFixture<SignUpPageComponent>;
  let mockApi: { submitSignUpRequest: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    mockApi = {
      submitSignUpRequest: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [SignUpPageComponent],
      providers: [provideRouter([]), { provide: OnboardingApiService, useValue: mockApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(SignUpPageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call OnboardingApiService.submitSignUpRequest on form submit', () => {
    mockApi.submitSignUpRequest.mockReturnValue(
      of({
        id: '123',
        salonName: 'Salon',
        ownerFirstName: 'Jan',
        ownerLastName: 'Jansen',
        email: 'jan@test.nl',
        createdAtUtc: '2026-01-01T00:00:00Z',
      }),
    );

    component['form'].patchValue({
      salonName: 'Salon',
      ownerFirstName: 'Jan',
      ownerLastName: 'Jansen',
      email: 'jan@test.nl',
    });

    component['onSubmit']();

    expect(mockApi.submitSignUpRequest).toHaveBeenCalledWith({
      salonName: 'Salon',
      ownerFirstName: 'Jan',
      ownerLastName: 'Jansen',
      email: 'jan@test.nl',
      phoneNumber: null,
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
    mockApi.submitSignUpRequest.mockReturnValue(
      of({
        id: '123',
        salonName: 'Salon',
        ownerFirstName: 'Jan',
        ownerLastName: 'Jansen',
        email: 'jan@test.nl',
        createdAtUtc: '2026-01-01T00:00:00Z',
      }),
    );

    component['form'].patchValue({
      salonName: 'Salon',
      ownerFirstName: 'Jan',
      ownerLastName: 'Jansen',
      email: 'jan@test.nl',
    });

    component['onSubmit']();

    expect(navigateSpy).toHaveBeenCalledWith(['/bevestiging'], {
      queryParams: { type: 'aanmelding' },
    });
  });

  it('should display generic error message on server failure', () => {
    mockApi.submitSignUpRequest.mockReturnValue(throwError(() => new Error('Server error')));

    component['form'].patchValue({
      salonName: 'Salon',
      ownerFirstName: 'Jan',
      ownerLastName: 'Jansen',
      email: 'jan@test.nl',
    });

    component['onSubmit']();
    fixture.detectChanges();

    expect(component['submitError']()).toBe(
      'Er is een fout opgetreden. Probeer het later opnieuw.',
    );
  });
});
