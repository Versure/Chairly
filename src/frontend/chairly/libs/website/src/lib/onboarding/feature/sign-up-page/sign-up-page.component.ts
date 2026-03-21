import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { OnboardingApiService } from '../../data-access';
import { SubmitSignUpRequestPayload } from '../../models';
import { FooterComponent, HeaderComponent } from '../../ui';

@Component({
  selector: 'chairly-web-sign-up-page',
  standalone: true,
  imports: [ReactiveFormsModule, HeaderComponent, FooterComponent],
  templateUrl: './sign-up-page.component.html',
  styleUrl: './sign-up-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPageComponent {
  private readonly onboardingApi = inject(OnboardingApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

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

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const formValue = this.form.getRawValue();
    const payload: SubmitSignUpRequestPayload = {
      salonName: formValue.salonName,
      ownerFirstName: formValue.ownerFirstName,
      ownerLastName: formValue.ownerLastName,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber || null,
    };

    this.onboardingApi
      .submitSignUpRequest(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/bevestiging'], {
            queryParams: { type: 'aanmelding' },
          });
        },
        error: () => {
          this.isSubmitting.set(false);
          this.submitError.set('Er is een fout opgetreden. Probeer het later opnieuw.');
        },
      });
  }
}
