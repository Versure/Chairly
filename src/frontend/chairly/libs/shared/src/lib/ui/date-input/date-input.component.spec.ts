import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';

import { DateInputComponent } from './date-input.component';

describe('DateInputComponent', () => {
  let component: DateInputComponent;
  let fixture: ComponentFixture<DateInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DateInputComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DateInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function getTriggerButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button[role="combobox"]') as HTMLButtonElement;
  }

  function getPopover(): HTMLDivElement | null {
    return fixture.nativeElement.querySelector('.absolute.z-10') as HTMLDivElement | null;
  }

  function getNativeInput(): HTMLInputElement | null {
    return fixture.nativeElement.querySelector('input') as HTMLInputElement | null;
  }

  function getConfirmButton(): HTMLButtonElement | null {
    const buttons = fixture.nativeElement.querySelectorAll(
      'button[type="button"]',
    ) as NodeListOf<HTMLButtonElement>;
    return Array.from(buttons).find((b) => b.textContent?.trim() === 'Bevestigen') ?? null;
  }

  function getCancelButton(): HTMLButtonElement | null {
    const buttons = fixture.nativeElement.querySelectorAll(
      'button[type="button"]',
    ) as NodeListOf<HTMLButtonElement>;
    return Array.from(buttons).find((b) => b.textContent?.trim() === 'Annuleren') ?? null;
  }

  it('should render placeholder when no value is set', () => {
    fixture.componentRef.setInput('placeholder', 'Kies een datum');
    fixture.detectChanges();

    const trigger = getTriggerButton();
    expect(trigger.textContent).toContain('Kies een datum');
  });

  it('should display formatted date value (DD-MM-YYYY)', () => {
    component.writeValue('2026-03-12');
    fixture.detectChanges();

    const trigger = getTriggerButton();
    expect(trigger.textContent).toContain('12-03-2026');
  });

  it('should open popover on click with native input and both buttons', () => {
    const trigger = getTriggerButton();
    trigger.click();
    fixture.detectChanges();

    expect(getPopover()).toBeTruthy();
    expect(getNativeInput()).toBeTruthy();
    expect(getConfirmButton()).toBeTruthy();
    expect(getCancelButton()).toBeTruthy();
  });

  it('should commit value on Bevestigen click', () => {
    let emittedValue = '';
    component.registerOnChange((v: string) => {
      emittedValue = v;
    });

    component.writeValue('2026-01-01');
    fixture.detectChanges();

    // Open popover
    getTriggerButton().click();
    fixture.detectChanges();

    // Change native input value
    const nativeInput = getNativeInput();
    expect(nativeInput).toBeTruthy();
    nativeInput!.value = '2026-06-15'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
    nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
    fixture.detectChanges();

    // Click Bevestigen
    getConfirmButton()?.click();
    fixture.detectChanges();

    expect(emittedValue).toBe('2026-06-15');
    expect(getPopover()).toBeNull();
  });

  it('should restore value on Annuleren click', () => {
    let emittedValue = 'initial';
    component.registerOnChange((v: string) => {
      emittedValue = v;
    });

    component.writeValue('2026-01-01');
    fixture.detectChanges();

    // Open popover
    getTriggerButton().click();
    fixture.detectChanges();

    // Change native input value
    const nativeInput = getNativeInput();
    expect(nativeInput).toBeTruthy();
    nativeInput!.value = '2026-06-15'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
    nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
    fixture.detectChanges();

    // Click Annuleren
    getCancelButton()?.click();
    fixture.detectChanges();

    // Value should not have been committed
    expect(emittedValue).toBe('initial');
    expect(getPopover()).toBeNull();

    // Trigger should still show original value
    const trigger = getTriggerButton();
    expect(trigger.textContent).toContain('01-01-2026');
  });

  it('should close without committing on Escape', () => {
    let emittedValue = 'initial';
    component.registerOnChange((v: string) => {
      emittedValue = v;
    });

    component.writeValue('2026-01-01');
    fixture.detectChanges();

    // Open popover
    getTriggerButton().click();
    fixture.detectChanges();

    // Change native input value
    const nativeInput = getNativeInput();
    expect(nativeInput).toBeTruthy();
    nativeInput!.value = '2026-06-15'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
    nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
    fixture.detectChanges();

    // Press Escape on the trigger button
    const trigger = getTriggerButton();
    trigger.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(emittedValue).toBe('initial');
    expect(getPopover()).toBeNull();
  });

  it('should close without committing on outside click', () => {
    let emittedValue = 'initial';
    component.registerOnChange((v: string) => {
      emittedValue = v;
    });

    component.writeValue('2026-01-01');
    fixture.detectChanges();

    // Open popover
    getTriggerButton().click();
    fixture.detectChanges();

    // Change native input value
    const nativeInput = getNativeInput();
    expect(nativeInput).toBeTruthy();
    nativeInput!.value = '2026-06-15'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
    nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
    fixture.detectChanges();

    // Click outside the component
    document.body.click();
    fixture.detectChanges();

    expect(emittedValue).toBe('initial');
    expect(getPopover()).toBeNull();
  });

  describe('ControlValueAccessor with reactive forms', () => {
    it('should work with FormControl', () => {
      const control = new FormControl('2026-03-12');

      component.registerOnChange((v: string) => control.setValue(v));
      component.writeValue(control.value ?? '');
      fixture.detectChanges();

      const trigger = getTriggerButton();
      expect(trigger.textContent).toContain('12-03-2026');

      // Open, change, confirm
      trigger.click();
      fixture.detectChanges();

      const nativeInput = getNativeInput();
      expect(nativeInput).toBeTruthy();
      nativeInput!.value = '2026-07-20'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
      nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
      fixture.detectChanges();

      getConfirmButton()?.click();
      fixture.detectChanges();

      expect(control.value).toBe('2026-07-20');
    });
  });

  describe('ControlValueAccessor with ngModel', () => {
    it('should support ngModel via writeValue and registerOnChange', () => {
      // Simulate ngModel: writeValue sets the initial value, registerOnChange captures changes
      let modelValue = '2026-03-12';
      component.registerOnChange((v: string) => {
        modelValue = v;
      });
      component.writeValue(modelValue);
      fixture.detectChanges();

      const trigger = getTriggerButton();
      expect(trigger.textContent).toContain('12-03-2026');

      // Open, change, confirm
      trigger.click();
      fixture.detectChanges();

      const nativeInput = getNativeInput();
      expect(nativeInput).toBeTruthy();
      nativeInput!.value = '2026-08-25'; // eslint-disable-line @typescript-eslint/no-non-null-assertion
      nativeInput!.dispatchEvent(new Event('input')); // eslint-disable-line @typescript-eslint/no-non-null-assertion
      fixture.detectChanges();

      getConfirmButton()?.click();
      fixture.detectChanges();

      expect(modelValue).toBe('2026-08-25');
    });
  });

  it('should support all three input types', () => {
    // date
    fixture.componentRef.setInput('type', 'date');
    fixture.detectChanges();
    getTriggerButton().click();
    fixture.detectChanges();
    const dateInput = getNativeInput();
    expect(dateInput).toBeTruthy();
    expect(dateInput?.type).toBe('date');
    getTriggerButton().click();
    fixture.detectChanges();

    // datetime-local
    fixture.componentRef.setInput('type', 'datetime-local');
    fixture.detectChanges();
    getTriggerButton().click();
    fixture.detectChanges();
    const dtInput = getNativeInput();
    expect(dtInput).toBeTruthy();
    expect(dtInput?.type).toBe('datetime-local');
    getTriggerButton().click();
    fixture.detectChanges();

    // time
    fixture.componentRef.setInput('type', 'time');
    fixture.detectChanges();
    getTriggerButton().click();
    fixture.detectChanges();
    const timeInput = getNativeInput();
    expect(timeInput).toBeTruthy();
    expect(timeInput?.type).toBe('time');
  });

  it('should format datetime-local value as DD-MM-YYYY HH:mm', () => {
    fixture.componentRef.setInput('type', 'datetime-local');
    component.writeValue('2026-03-12T14:30');
    fixture.detectChanges();

    const trigger = getTriggerButton();
    expect(trigger.textContent).toContain('12-03-2026 14:30');
  });

  it('should format time value as HH:mm', () => {
    fixture.componentRef.setInput('type', 'time');
    component.writeValue('14:30');
    fixture.detectChanges();

    const trigger = getTriggerButton();
    expect(trigger.textContent).toContain('14:30');
  });
});
