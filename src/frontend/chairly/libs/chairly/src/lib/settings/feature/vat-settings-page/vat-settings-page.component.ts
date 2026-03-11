import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

import { SettingsApiService } from '../../data-access';

@Component({
  selector: 'chairly-vat-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './vat-settings-page.component.html',
})
export class VatSettingsPageComponent implements OnInit {
  private readonly settingsApi = inject(SettingsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly saveSuccess = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected readonly defaultVatRateControl = new FormControl<number>(21, {
    nonNullable: true,
    validators: [Validators.required],
  });

  ngOnInit(): void {
    this.loadSettings();
  }

  private loadSettings(): void {
    this.isLoading.set(true);
    this.settingsApi
      .getVatSettings()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings) => {
          this.defaultVatRateControl.setValue(settings.defaultVatRate);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }

  protected onSave(): void {
    if (this.defaultVatRateControl.invalid) {
      return;
    }
    this.isSaving.set(true);
    this.saveSuccess.set(false);
    this.saveError.set(null);

    this.settingsApi
      .updateVatSettings(this.defaultVatRateControl.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (settings) => {
          this.defaultVatRateControl.setValue(settings.defaultVatRate);
          this.isSaving.set(false);
          this.saveSuccess.set(true);
        },
        error: (err: unknown) => {
          this.isSaving.set(false);
          this.saveError.set(err instanceof Error ? err.message : String(err));
        },
      });
  }
}
