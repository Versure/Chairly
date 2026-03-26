import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  forwardRef,
  inject,
  input,
  viewChild,
  ViewEncapsulation,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import flatpickr from 'flatpickr';
import { Dutch } from 'flatpickr/dist/l10n/nl';
import confirmDatePlugin from 'flatpickr/dist/plugins/confirmDate/confirmDate';
import { Instance } from 'flatpickr/dist/types/instance';

// eslint-disable-next-line @typescript-eslint/no-empty-function -- ControlValueAccessor default no-op
const noop = (): void => {};

@Component({
  selector: 'chairly-date-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true,
    },
  ],
  templateUrl: './date-picker.component.html',
  styleUrls: ['./date-picker.css', './date-picker.component.scss'],
})
export class DatePickerComponent implements ControlValueAccessor, AfterViewInit {
  readonly mode = input<'date' | 'datetime' | 'time'>('date');
  readonly inputId = input<string>('');
  readonly placeholder = input<string>('');
  readonly minDate = input<string | Date | undefined>(undefined);
  readonly maxDate = input<string | Date | undefined>(undefined);
  readonly disabledDates = input<Array<string | Date | { from: string | Date; to: string | Date }>>(
    [],
  );
  private readonly pickerInput = viewChild.required<ElementRef<HTMLInputElement>>('pickerInput');
  private readonly destroyRef = inject(DestroyRef);

  private flatpickrInstance: Instance | null = null;
  private onChange: (value: string) => void = noop;
  private onTouched: () => void = noop;
  private pendingValue: string | null = null;

  constructor() {
    effect(() => {
      const min = this.minDate();
      if (this.flatpickrInstance) {
        this.flatpickrInstance.set('minDate', min ?? undefined);
      }
    });

    effect(() => {
      const max = this.maxDate();
      if (this.flatpickrInstance) {
        this.flatpickrInstance.set('maxDate', max ?? undefined);
      }
    });

    effect(() => {
      const disabled = this.disabledDates();
      if (this.flatpickrInstance) {
        this.flatpickrInstance.set('disable', disabled);
      }
    });
  }

  ngAfterViewInit(): void {
    this.initFlatpickr();
  }

  writeValue(value: string): void {
    if (this.flatpickrInstance) {
      if (value) {
        this.flatpickrInstance.setDate(value, false);
      } else {
        this.flatpickrInstance.clear(false);
      }
    } else {
      this.pendingValue = value;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  private initFlatpickr(): void {
    const currentMode = this.mode();
    const enableTime = currentMode === 'datetime' || currentMode === 'time';
    const noCalendar = currentMode === 'time';

    const config = this.getFormatConfig(currentMode);

    const originalInput = this.pickerInput().nativeElement;
    const originalId = originalInput.id;

    this.flatpickrInstance = flatpickr(originalInput, {
      locale: Dutch,
      enableTime,
      noCalendar,
      altInput: true,
      altFormat: config.altFormat,
      dateFormat: config.dateFormat,
      time_24hr: true,
      static: true,
      minDate: this.minDate() ?? undefined,
      maxDate: this.maxDate() ?? undefined,
      disable: this.disabledDates(),
      plugins: [
        confirmDatePlugin({
          confirmText: 'Bevestigen',
          showAlways: true,
          theme: 'light',
        }),
      ],
      onClose: (selectedDates: Date[], dateStr: string) => {
        this.commitValue(selectedDates, dateStr);
      },
    }) as Instance;

    // Transfer the id from the hidden original input to the visible alt input so
    // that <label for="..."> associations (and Playwright getByLabel queries) resolve
    // to the visible control instead of the hidden one.
    if (originalId) {
      const altInput = this.flatpickrInstance.altInput;
      if (altInput) {
        originalInput.removeAttribute('id');
        altInput.id = originalId;
      }
    }

    if (this.pendingValue) {
      this.flatpickrInstance.setDate(this.pendingValue, false);
      this.pendingValue = null;
    }

    this.destroyRef.onDestroy(() => {
      this.flatpickrInstance?.destroy();
      this.flatpickrInstance = null;
    });
  }

  private getFormatConfig(mode: 'date' | 'datetime' | 'time'): {
    altFormat: string;
    dateFormat: string;
  } {
    switch (mode) {
      case 'datetime':
        return { altFormat: 'd-m-Y H:i', dateFormat: 'Y-m-d\\TH:i:S' };
      case 'time':
        return { altFormat: 'H:i', dateFormat: 'H:i' };
      default:
        return { altFormat: 'd-m-Y', dateFormat: 'Y-m-d' };
    }
  }

  private commitValue(selectedDates: Date[], dateStr: string): void {
    if (selectedDates.length > 0) {
      this.onChange(dateStr);
    }
    this.onTouched();
  }
}
