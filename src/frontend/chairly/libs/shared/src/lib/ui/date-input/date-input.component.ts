import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  forwardRef,
  HostListener,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

// eslint-disable-next-line @typescript-eslint/no-empty-function -- ControlValueAccessor default no-op
const noop = (): void => {};

@Component({
  selector: 'chairly-date-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateInputComponent),
      multi: true,
    },
  ],
  templateUrl: './date-input.component.html',
})
export class DateInputComponent implements ControlValueAccessor {
  readonly type = input<'date' | 'datetime-local' | 'time'>('date');
  readonly inputId = input<string>('');
  readonly placeholder = input<string>('');

  protected readonly value = signal<string>('');
  protected readonly tempValue = signal<string>('');
  protected readonly isOpen = signal<boolean>(false);

  protected readonly nativeInput = viewChild<ElementRef<HTMLInputElement>>('nativeInput');

  private onChange: (value: string) => void = noop;
  private onTouched: () => void = noop;

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  private static nextId = 0;
  private readonly instanceId = DateInputComponent.nextId++;

  protected readonly popoverId = computed<string>(() => `date-input-popover-${this.instanceId}`);

  protected readonly displayValue = computed<string>(() => {
    const val = this.value();
    if (!val) {
      return '';
    }

    const currentType = this.type();
    if (currentType === 'date') {
      return this.formatDate(val);
    }
    if (currentType === 'datetime-local') {
      return this.formatDateTimeLocal(val);
    }
    // type === 'time'
    return val;
  });

  protected readonly icon = computed<string>(() => {
    if (this.type() === 'time') {
      return 'clock';
    }
    return 'calendar';
  });

  writeValue(value: string): void {
    this.value.set(value ?? '');
    this.tempValue.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  @HostListener('document:click', ['$event.target'])
  protected onDocumentClick(target: EventTarget | null): void {
    if (!target || !this.elementRef.nativeElement.contains(target as Node)) {
      this.cancel();
    }
  }

  protected toggle(): void {
    if (this.isOpen()) {
      this.cancel();
    } else {
      this.tempValue.set(this.value());
      this.isOpen.set(true);
      setTimeout(() => {
        const inputEl = this.nativeInput();
        if (inputEl) {
          inputEl.nativeElement.focus();
        }
      });
    }
  }

  protected onNativeInput(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.tempValue.set(val);
  }

  protected confirm(): void {
    const newValue = this.tempValue();
    this.value.set(newValue);
    this.onChange(newValue);
    this.onTouched();
    this.isOpen.set(false);
  }

  protected cancel(): void {
    if (this.isOpen()) {
      this.tempValue.set(this.value());
      this.onTouched();
      this.isOpen.set(false);
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.cancel();
    }
  }

  private formatDate(value: string): string {
    const parts = value.split('-');
    if (parts.length !== 3) {
      return value;
    }
    return `${parts[2]}-${parts[1]}-${parts[0]}`;
  }

  private formatDateTimeLocal(value: string): string {
    const [datePart, timePart] = value.split('T');
    if (!datePart || !timePart) {
      return value;
    }
    const formattedDate = this.formatDate(datePart);
    const timeShort = timePart.substring(0, 5);
    return `${formattedDate} ${timeShort}`;
  }
}
