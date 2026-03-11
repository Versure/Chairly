import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { DropdownOption } from './dropdown-option.model';
import { SearchableDropdownComponent } from './searchable-dropdown.component';

const mockOptions: DropdownOption[] = [
  { id: 'opt-1', label: 'Jan Jansen' },
  { id: 'opt-2', label: 'Piet Pietersen' },
  { id: 'opt-3', label: 'Anna de Vries' },
];

describe('SearchableDropdownComponent', () => {
  let component: SearchableDropdownComponent;
  let fixture: ComponentFixture<SearchableDropdownComponent>;
  let inputEl: HTMLInputElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchableDropdownComponent, ReactiveFormsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchableDropdownComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('options', mockOptions);
    fixture.detectChanges();

    inputEl = fixture.nativeElement.querySelector('input') as HTMLInputElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render input with default placeholder', () => {
    expect(inputEl.placeholder).toBe('Zoeken...');
  });

  it('should render input with custom placeholder', () => {
    fixture.componentRef.setInput('placeholder', 'Klant zoeken...');
    fixture.detectChanges();

    expect(inputEl.placeholder).toBe('Klant zoeken...');
  });

  it('should not set id attribute when inputId is not provided', () => {
    expect(inputEl.id).toBe('');
  });

  it('should set id attribute on inner input when inputId is provided', () => {
    fixture.componentRef.setInput('inputId', 'bfd-clientId');
    fixture.detectChanges();

    expect(inputEl.id).toBe('bfd-clientId');
  });

  it('should open dropdown and show all options on focus', () => {
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    expect(listItems.length).toBe(3);
    expect(listItems[0].textContent?.trim()).toBe('Jan Jansen');
    expect(listItems[1].textContent?.trim()).toBe('Piet Pietersen');
    expect(listItems[2].textContent?.trim()).toBe('Anna de Vries');
  });

  it('should filter options as user types (case-insensitive)', () => {
    inputEl.dispatchEvent(new Event('focus'));
    inputEl.value = 'jan';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    expect(listItems.length).toBe(1);
    expect(listItems[0].textContent?.trim()).toBe('Jan Jansen');
  });

  it('should filter options matching anywhere in label', () => {
    inputEl.dispatchEvent(new Event('focus'));
    inputEl.value = 'Vries';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    expect(listItems.length).toBe(1);
    expect(listItems[0].textContent?.trim()).toBe('Anna de Vries');
  });

  it('should show "Geen resultaten gevonden" when no match', () => {
    inputEl.dispatchEvent(new Event('focus'));
    inputEl.value = 'xyz';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    expect(listItems.length).toBe(1);
    expect(listItems[0].textContent?.trim()).toBe('Geen resultaten gevonden');
  });

  it('should select option on click and emit correct id value', () => {
    let emittedValue = '';
    component.registerOnChange((value: string) => {
      emittedValue = value;
    });

    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    listItems[1].click();
    fixture.detectChanges();

    expect(emittedValue).toBe('opt-2');
    expect(inputEl.value).toBe('Piet Pietersen');
  });

  it('should close dropdown after selecting an option', () => {
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    listItems[0].click();
    fixture.detectChanges();

    const dropdown = fixture.nativeElement.querySelector('ul');
    expect(dropdown).toBeNull();
  });

  describe('keyboard navigation', () => {
    beforeEach(() => {
      inputEl.dispatchEvent(new Event('focus'));
      fixture.detectChanges();
    });

    it('should highlight next option on ArrowDown', () => {
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      expect(listItems[0].classList.contains('bg-primary-100')).toBe(true);
    });

    it('should highlight previous option on ArrowUp', () => {
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      expect(listItems[0].classList.contains('bg-primary-100')).toBe(true);
      expect(listItems[1].classList.contains('bg-primary-100')).toBe(false);
    });

    it('should select highlighted option on Enter', () => {
      let emittedValue = '';
      component.registerOnChange((value: string) => {
        emittedValue = value;
      });

      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      fixture.detectChanges();

      expect(emittedValue).toBe('opt-2');
      expect(inputEl.value).toBe('Piet Pietersen');
    });

    it('should close dropdown on Escape', () => {
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();

      const dropdown = fixture.nativeElement.querySelector('ul');
      expect(dropdown).toBeNull();
    });

    it('should not go below last option on ArrowDown', () => {
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' })); // extra
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      expect(listItems[2].classList.contains('bg-primary-100')).toBe(true);
    });

    it('should not go above first option on ArrowUp', () => {
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' }));
      inputEl.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' })); // extra
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      expect(listItems[0].classList.contains('bg-primary-100')).toBe(true);
    });
  });

  it('should close on outside click', () => {
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ul')).toBeTruthy();

    // Simulate a click outside the component
    document.body.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('ul')).toBeNull();
  });

  describe('ControlValueAccessor', () => {
    it('should set form control value when option is selected', () => {
      const control = new FormControl('');
      component.registerOnChange((value: string) => control.setValue(value));

      inputEl.dispatchEvent(new Event('focus'));
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      listItems[2].click();
      fixture.detectChanges();

      expect(control.value).toBe('opt-3');
    });

    it('should display matching label when writeValue is called with existing id', () => {
      component.writeValue('opt-3');
      fixture.detectChanges();

      expect(inputEl.value).toBe('Anna de Vries');
    });

    it('should clear display when writeValue is called with empty string', () => {
      component.writeValue('opt-1');
      fixture.detectChanges();
      expect(inputEl.value).toBe('Jan Jansen');

      component.writeValue('');
      fixture.detectChanges();
      expect(inputEl.value).toBe('');
    });

    it('should call onTouched when dropdown closes', () => {
      const touchedSpy = vi.fn();
      component.registerOnTouched(touchedSpy);

      inputEl.dispatchEvent(new Event('focus'));
      fixture.detectChanges();

      // Close via outside click
      document.body.click();
      fixture.detectChanges();

      expect(touchedSpy).toHaveBeenCalled();
    });

    it('should call onTouched when option is selected', () => {
      const touchedSpy = vi.fn();
      component.registerOnTouched(touchedSpy);

      inputEl.dispatchEvent(new Event('focus'));
      fixture.detectChanges();

      const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
      listItems[0].click();
      fixture.detectChanges();

      expect(touchedSpy).toHaveBeenCalled();
    });
  });

  it('should respect disabled state', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    expect(inputEl.disabled).toBe(true);

    // Focus should not open dropdown when disabled
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    const dropdown = fixture.nativeElement.querySelector('ul');
    expect(dropdown).toBeNull();
  });

  it('should clear selection when user types after selecting', () => {
    let emittedValue = '';
    component.registerOnChange((value: string) => {
      emittedValue = value;
    });

    // Select an option first
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    const listItems = fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLLIElement>;
    listItems[0].click();
    fixture.detectChanges();
    expect(emittedValue).toBe('opt-1');

    // Now start typing again
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    inputEl.value = 'P';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(emittedValue).toBe('');
  });

  it('should restore selected label when dropdown closes without new selection', () => {
    component.writeValue('opt-1');
    fixture.detectChanges();
    expect(inputEl.value).toBe('Jan Jansen');

    // Open and type something but do not select
    inputEl.dispatchEvent(new Event('focus'));
    fixture.detectChanges();
    inputEl.value = 'xyz';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    // Close via outside click
    document.body.click();
    fixture.detectChanges();

    // Should restore the previously selected label
    expect(inputEl.value).toBe('Jan Jansen');
  });
});
