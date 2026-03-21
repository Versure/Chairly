import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { OnboardingApiService } from '../../data-access';
import { SubmitDemoRequestPayload } from '../../models';
import { FooterComponent, HeaderComponent } from '../../ui';

@Component({
  selector: 'chairly-web-demo-request-page',
  standalone: true,
  imports: [ReactiveFormsModule, HeaderComponent, FooterComponent],
  templateUrl: './demo-request-page.component.html',
  styleUrl: './demo-request-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DemoRequestPageComponent {
  private readonly onboardingApi = inject(OnboardingApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    contactName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    salonName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(256)],
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(50)],
    }),
    message: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(2000)],
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
    const payload: SubmitDemoRequestPayload = {
      contactName: formValue.contactName,
      salonName: formValue.salonName,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber || null,
      message: formValue.message || null,
    };

    this.onboardingApi
      .submitDemoRequest(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          void this.router.navigate(['/bevestiging'], {
            queryParams: { type: 'demo' },
          });
        },
        error: () => {
          this.isSubmitting.set(false);
          this.submitError.set('Er is een fout opgetreden. Probeer het later opnieuw.');
        },
      });
  }
}
