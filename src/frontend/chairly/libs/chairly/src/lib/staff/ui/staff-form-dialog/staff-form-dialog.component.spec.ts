import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateStaffMemberRequest, StaffMemberResponse } from '../../models';
import { StaffFormDialogComponent } from './staff-form-dialog.component';

const mockStaffMember: StaffMemberResponse = {
  id: 'staff-1',
  firstName: 'Jan',
  lastName: 'de Vries',
  email: 'jan.devries@salon.nl',
  role: 'manager',
  color: '#8b5cf6',
  photoUrl: null,
  isActive: true,
  schedule: {},
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

describe('StaffFormDialogComponent', () => {
  let component: StaffFormDialogComponent;
  let fixture: ComponentFixture<StaffFormDialogComponent>;
  let dialogEl: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffFormDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    dialogEl = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    dialogEl.showModal = vi.fn();
    dialogEl.close = vi.fn();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render empty form in create mode', () => {
    component.open();
    fixture.detectChanges();

    const firstNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="firstName"]',
    ) as HTMLInputElement;
    const lastNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="lastName"]',
    ) as HTMLInputElement;
    const emailInput = fixture.nativeElement.querySelector(
      'input[formControlName="email"]',
    ) as HTMLInputElement;

    expect(firstNameInput.value).toBe('');
    expect(lastNameInput.value).toBe('');
    expect(emailInput.value).toBe('');
  });

  it('should pre-fill form values in edit mode', () => {
    fixture.componentRef.setInput('staffMember', mockStaffMember);
    fixture.detectChanges();
    component.open();
    fixture.detectChanges();

    const firstNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="firstName"]',
    ) as HTMLInputElement;
    const lastNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="lastName"]',
    ) as HTMLInputElement;
    const roleSelect = fixture.nativeElement.querySelector(
      'select[formControlName="role"]',
    ) as HTMLSelectElement;
    const emailInput = fixture.nativeElement.querySelector(
      'input[formControlName="email"]',
    ) as HTMLInputElement;

    expect(firstNameInput.value).toBe('Jan');
    expect(lastNameInput.value).toBe('de Vries');
    expect(emailInput.value).toBe('jan.devries@salon.nl');
    expect(roleSelect.value).toBe('manager');
  });

  it('should disable Opslaan button when form is invalid', () => {
    component.open();
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;

    expect(submitButton.disabled).toBe(true);
  });

  it('should emit save event with correct payload on valid submit', () => {
    let emitted: CreateStaffMemberRequest | undefined;
    component.saved.subscribe((req) => {
      emitted = req;
    });

    component.open();

    const firstNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="firstName"]',
    ) as HTMLInputElement;
    firstNameInput.value = 'Anna';
    firstNameInput.dispatchEvent(new Event('input'));

    const lastNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="lastName"]',
    ) as HTMLInputElement;
    lastNameInput.value = 'Bakker';
    lastNameInput.dispatchEvent(new Event('input'));

    const emailInput = fixture.nativeElement.querySelector(
      'input[formControlName="email"]',
    ) as HTMLInputElement;
    emailInput.value = 'anna.bakker@salon.nl';
    emailInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toBeDefined();
    expect(emitted?.firstName).toBe('Anna');
    expect(emitted?.lastName).toBe('Bakker');
    expect(emitted?.email).toBe('anna.bakker@salon.nl');
    expect(emitted?.role).toBe('staff_member');
    expect(emitted?.color).toBe('#6366f1');
  });

  it('should show required validation message for email', () => {
    component.open();
    fixture.detectChanges();

    const emailInput = fixture.nativeElement.querySelector(
      'input[formControlName="email"]',
    ) as HTMLInputElement;
    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('E-mailadres is verplicht.');
  });

  it('should show format validation message for invalid email', () => {
    component.open();
    fixture.detectChanges();

    const emailInput = fixture.nativeElement.querySelector(
      'input[formControlName="email"]',
    ) as HTMLInputElement;
    emailInput.value = 'ongeldig';
    emailInput.dispatchEvent(new Event('input'));
    emailInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Voer een geldig e-mailadres in.');
  });

  it('should show API validation message when apiError input is set', () => {
    fixture.componentRef.setInput(
      'apiError',
      'Controleer het e-mailadres. Dit veld is verplicht en moet een geldig formaat hebben.',
    );
    component.open();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'Controleer het e-mailadres. Dit veld is verplicht en moet een geldig formaat hebben.',
    );
  });

  it('should emit cancel event on Annuleren click', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    component.open();

    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button[type="button"]'),
    ) as HTMLButtonElement[];
    const cancelButton = buttons.find(
      (b) => b.textContent?.trim() === 'Annuleren',
    ) as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    expect(cancelled).toBe(true);
    expect(dialogEl.close).toHaveBeenCalled();
  });
});
