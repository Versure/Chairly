import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { SettingsApiService } from '../../data-access';
import { CompanyInfo, UpdateCompanyInfoRequest } from '../../models';

@Component({
  selector: 'chairly-company-info-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, LoadingIndicatorComponent],
  templateUrl: './company-info-page.component.html',
})
export class CompanyInfoPageComponent implements OnInit {
  private readonly settingsApi = inject(SettingsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly saveSuccess = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    companyName: new FormControl<string | null>(null),
    companyEmail: new FormControl<string | null>(null),
    companyAddress: new FormControl<string | null>(null),
    companyPhone: new FormControl<string | null>(null),
    ibanNumber: new FormControl<string | null>(null),
    vatNumber: new FormControl<string | null>(null),
    paymentPeriodDays: new FormControl<number | null>(null),
  });

  ngOnInit(): void {
    this.loadCompanyInfo();
  }

  private loadCompanyInfo(): void {
    this.isLoading.set(true);
    this.settingsApi
      .getCompanyInfo()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (info: CompanyInfo) => {
          this.form.patchValue({
            companyName: info.companyName,
            companyEmail: info.companyEmail,
            companyAddress: info.companyAddress,
            companyPhone: info.companyPhone,
            ibanNumber: info.ibanNumber,
            vatNumber: info.vatNumber,
            paymentPeriodDays: info.paymentPeriodDays,
          });
          this.isLoading.set(false);
        },
        error: (err: unknown) => {
          this.saveError.set(err instanceof Error ? err.message : String(err));
          this.isLoading.set(false);
        },
      });
  }

  protected onSubmit(): void {
    if (this.form.invalid || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);
    this.saveSuccess.set(false);
    this.saveError.set(null);

    const request: UpdateCompanyInfoRequest = {
      companyName: this.form.value.companyName ?? null,
      companyEmail: this.form.value.companyEmail ?? null,
      companyAddress: this.form.value.companyAddress ?? null,
      companyPhone: this.form.value.companyPhone ?? null,
      ibanNumber: this.form.value.ibanNumber ?? null,
      vatNumber: this.form.value.vatNumber ?? null,
      paymentPeriodDays: this.form.value.paymentPeriodDays ?? null,
    };

    this.settingsApi
      .updateCompanyInfo(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (info: CompanyInfo) => {
          this.form.patchValue({
            companyName: info.companyName,
            companyEmail: info.companyEmail,
            companyAddress: info.companyAddress,
            companyPhone: info.companyPhone,
            ibanNumber: info.ibanNumber,
            vatNumber: info.vatNumber,
            paymentPeriodDays: info.paymentPeriodDays,
          });
          this.isSaving.set(false);
          this.saveSuccess.set(true);
          setTimeout(() => this.saveSuccess.set(false), 3000);
        },
        error: (err: unknown) => {
          this.saveError.set(err instanceof Error ? err.message : String(err));
          this.isSaving.set(false);
        },
      });
  }
}
