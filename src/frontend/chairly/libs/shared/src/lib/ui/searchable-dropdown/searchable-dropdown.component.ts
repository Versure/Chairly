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
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { DropdownOption } from './dropdown-option.model';

// eslint-disable-next-line @typescript-eslint/no-empty-function -- ControlValueAccessor default no-op
const noop = (): void => {};

@Component({
  selector: 'chairly-searchable-dropdown',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableDropdownComponent),
      multi: true,
    },
  ],
  templateUrl: './searchable-dropdown.component.html',
})
export class SearchableDropdownComponent implements ControlValueAccessor {
  readonly options = input<DropdownOption[]>([]);
  readonly placeholder = input<string>('Zoeken...');
  readonly disabled = input<boolean>(false);
  readonly inputId = input<string>('');

  protected readonly searchText = signal<string>('');
  protected readonly isOpen = signal<boolean>(false);
  protected readonly highlightedIndex = signal<number>(-1);
  protected readonly selectedOption = signal<DropdownOption | null>(null);

  /** Tracks the last committed selection so we can restore it if the user dismisses without choosing. */
  private committedOption: DropdownOption | null = null;

  private onChange: (value: string) => void = noop;
  private onTouched: () => void = noop;

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly filteredOptions = computed<DropdownOption[]>(() => {
    const search = this.searchText().toLowerCase();
    if (!search) {
      return this.options();
    }
    return this.options().filter((option) => option.label.toLowerCase().includes(search));
  });

  protected readonly displayValue = computed<string>(() => {
    const selected = this.selectedOption();
    if (selected && !this.isOpen()) {
      return selected.label;
    }
    return this.searchText();
  });

  writeValue(value: string): void {
    if (value) {
      const match = this.options().find((o) => o.id === value);
      if (match) {
        this.selectedOption.set(match);
        this.committedOption = match;
        this.searchText.set(match.label);
      }
    } else {
      this.selectedOption.set(null);
      this.committedOption = null;
      this.searchText.set('');
    }
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
      this.closeDropdown();
    }
  }

  protected onInputFocus(): void {
    if (this.disabled()) {
      return;
    }
    this.isOpen.set(true);
    if (this.selectedOption()) {
      this.searchText.set('');
    }
  }

  protected onInputChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchText.set(value);
    this.isOpen.set(true);
    this.highlightedIndex.set(-1);

    if (this.selectedOption()) {
      this.selectedOption.set(null);
      this.onChange('');
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (this.disabled()) {
      return;
    }

    const filtered = this.filteredOptions();

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.isOpen()) {
          this.isOpen.set(true);
        }
        this.highlightedIndex.update((i) => (i < filtered.length - 1 ? i + 1 : i));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.highlightedIndex.update((i) => (i > 0 ? i - 1 : i));
        break;
      case 'Enter':
        event.preventDefault();
        if (
          this.isOpen() &&
          this.highlightedIndex() >= 0 &&
          this.highlightedIndex() < filtered.length
        ) {
          this.selectOption(filtered[this.highlightedIndex()]);
        }
        break;
      case 'Escape':
        event.preventDefault();
        this.closeDropdown();
        break;
    }
  }

  protected selectOption(option: DropdownOption): void {
    this.selectedOption.set(option);
    this.committedOption = option;
    this.searchText.set(option.label);
    this.onChange(option.id);
    this.onTouched();
    this.isOpen.set(false);
    this.highlightedIndex.set(-1);
  }

  private closeDropdown(): void {
    if (this.isOpen()) {
      this.isOpen.set(false);
      this.highlightedIndex.set(-1);
      this.onTouched();

      const selected = this.selectedOption();
      if (selected) {
        this.searchText.set(selected.label);
      } else if (this.committedOption) {
        // Restore the previously committed selection when closing without a new choice
        this.selectedOption.set(this.committedOption);
        this.searchText.set(this.committedOption.label);
        this.onChange(this.committedOption.id);
      } else {
        this.searchText.set('');
      }
    }
  }
}
