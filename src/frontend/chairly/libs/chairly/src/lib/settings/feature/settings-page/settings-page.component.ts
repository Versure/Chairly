import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { forkJoin } from 'rxjs';

import {
  LoadingIndicatorComponent,
  PageHeaderComponent,
  TemplateTypeLabelPipe,
} from '@org/shared-lib';

import { EmailTemplateApiService, EmailTemplateStore, SettingsApiService } from '../../data-access';
import {
  CompanyInfo,
  EmailTemplateResponse,
  UpdateCompanyInfoRequest,
  VatSettings,
} from '../../models';

@Component({
  selector: 'chairly-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    LoadingIndicatorComponent,
    PageHeaderComponent,
    TemplateTypeLabelPipe,
  ],
  providers: [EmailTemplateStore, EmailTemplateApiService],
  templateUrl: './settings-page.component.html',
})
export class SettingsPageComponent implements OnInit {
  private readonly settingsApi = inject(SettingsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly emailTemplateStore = inject(EmailTemplateStore);

  protected readonly isLoading = signal(false);
  protected readonly isSavingCompany = signal(false);
  protected readonly isSavingVat = signal(false);
  protected readonly saveCompanySuccess = signal(false);
  protected readonly saveCompanyError = signal<string | null>(null);
  protected readonly saveVatSuccess = signal(false);
  protected readonly saveVatError = signal<string | null>(null);

  protected readonly templates = computed<EmailTemplateResponse[]>(() =>
    this.emailTemplateStore.templates(),
  );
  protected readonly isLoadingTemplates = computed<boolean>(() =>
    this.emailTemplateStore.isLoading(),
  );

  protected readonly companyForm = new FormGroup({
    companyName: new FormControl<string | null>(null),
    companyEmail: new FormControl<string | null>(null),
    street: new FormControl<string | null>(null),
    houseNumber: new FormControl<string | null>(null),
    postalCode: new FormControl<string | null>(null),
    city: new FormControl<string | null>(null),
    companyPhone: new FormControl<string | null>(null),
    ibanNumber: new FormControl<string | null>(null),
    vatNumber: new FormControl<string | null>(null),
    paymentPeriodDays: new FormControl<number | null>(null),
  });

  protected readonly defaultVatRateControl = new FormControl<number>(21, {
    nonNullable: true,
    validators: [Validators.required],
  });

  ngOnInit(): void {
    this.loadSettings();
    this.emailTemplateStore.loadTemplates();
  }

  private loadSettings(): void {
    this.isLoading.set(true);
    forkJoin({
      company: this.settingsApi.getCompanyInfo(),
      vat: this.settingsApi.getVatSettings(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: { company: CompanyInfo; vat: VatSettings }) => {
          this.companyForm.patchValue({
            companyName: result.company.companyName,
            companyEmail: result.company.companyEmail,
            street: result.company.street,
            houseNumber: result.company.houseNumber,
            postalCode: result.company.postalCode,
            city: result.company.city,
            companyPhone: result.company.companyPhone,
            ibanNumber: result.company.ibanNumber,
            vatNumber: result.company.vatNumber,
            paymentPeriodDays: result.company.paymentPeriodDays,
          });
          this.defaultVatRateControl.setValue(result.vat.defaultVatRate);
          this.isLoading.set(false);
        },
        error: (err: unknown) => {
          this.saveCompanyError.set(err instanceof Error ? err.message : String(err));
          this.isLoading.set(false);
        },
      });
  }

  protected onSubmitCompany(): void {
    if (this.companyForm.invalid || this.isSavingCompany()) {
      return;
    }

    this.isSavingCompany.set(true);
    this.saveCompanySuccess.set(false);
    this.saveCompanyError.set(null);

    const request: UpdateCompanyInfoRequest = {
      companyName: this.companyForm.value.companyName ?? null,
      companyEmail: this.companyForm.value.companyEmail ?? null,
      street: this.companyForm.value.street ?? null,
      houseNumber: this.companyForm.value.houseNumber ?? null,
      postalCode: this.companyForm.value.postalCode ?? null,
      city: this.companyForm.value.city ?? null,
      companyPhone: this.companyForm.value.companyPhone ?? null,
      ibanNumber: this.companyForm.value.ibanNumber ?? null,
      vatNumber: this.companyForm.value.vatNumber ?? null,
      paymentPeriodDays: this.companyForm.value.paymentPeriodDays ?? null,
    };

    this.settingsApi
      .updateCompanyInfo(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (info: CompanyInfo) => {
          this.companyForm.patchValue({
            companyName: info.companyName,
            companyEmail: info.companyEmail,
            street: info.street,
            houseNumber: info.houseNumber,
            postalCode: info.postalCode,
            city: info.city,
            companyPhone: info.companyPhone,
            ibanNumber: info.ibanNumber,
            vatNumber: info.vatNumber,
            paymentPeriodDays: info.paymentPeriodDays,
          });
          this.isSavingCompany.set(false);
          this.saveCompanySuccess.set(true);
          setTimeout(() => this.saveCompanySuccess.set(false), 3000);
        },
        error: (err: unknown) => {
          this.saveCompanyError.set(err instanceof Error ? err.message : String(err));
          this.isSavingCompany.set(false);
        },
      });
  }

  protected onSubmitVat(): void {
    if (this.defaultVatRateControl.invalid || this.isSavingVat()) {
      return;
    }

    this.isSavingVat.set(true);
    this.saveVatSuccess.set(false);
    this.saveVatError.set(null);

    this.settingsApi
      .updateVatSettings(this.defaultVatRateControl.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings: VatSettings) => {
          this.defaultVatRateControl.setValue(settings.defaultVatRate);
          this.isSavingVat.set(false);
          this.saveVatSuccess.set(true);
          setTimeout(() => this.saveVatSuccess.set(false), 3000);
        },
        error: (err: unknown) => {
          this.saveVatError.set(err instanceof Error ? err.message : String(err));
          this.isSavingVat.set(false);
        },
      });
  }
}
