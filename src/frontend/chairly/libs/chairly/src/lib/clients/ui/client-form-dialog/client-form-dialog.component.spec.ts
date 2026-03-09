import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientResponse, CreateClientRequest } from '../../models';
import { ClientFormDialogComponent } from './client-form-dialog.component';

const mockClient: ClientResponse = {
  id: 'client-1',
  firstName: 'Anna',
  lastName: 'Bakker',
  email: 'anna@example.com',
  phoneNumber: null,
  notes: null,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
};

describe('ClientFormDialogComponent', () => {
  let component: ClientFormDialogComponent;
  let fixture: ComponentFixture<ClientFormDialogComponent>;
  let dialogEl: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientFormDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ClientFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    dialogEl = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    dialogEl.showModal = vi.fn();
    dialogEl.close = vi.fn();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show "Klant toevoegen" title in create mode', () => {
    component.open(null);
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h2') as HTMLHeadingElement;
    expect(heading.textContent?.trim()).toBe('Klant toevoegen');
  });

  it('should show "Klant bewerken" title in edit mode and pre-fill firstName/lastName', () => {
    fixture.componentRef.setInput('client', mockClient);
    fixture.detectChanges();
    component.open(mockClient);
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h2') as HTMLHeadingElement;
    expect(heading.textContent?.trim()).toBe('Klant bewerken');

    const firstNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="firstName"]',
    ) as HTMLInputElement;
    const lastNameInput = fixture.nativeElement.querySelector(
      'input[formControlName="lastName"]',
    ) as HTMLInputElement;

    expect(firstNameInput.value).toBe('Anna');
    expect(lastNameInput.value).toBe('Bakker');
  });

  it('should disable Opslaan button when form is invalid', () => {
    component.open(null);
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;

    expect(submitButton.disabled).toBe(true);
  });

  it('should emit correct payload on valid submit', () => {
    let emitted: CreateClientRequest | undefined;
    component.saved.subscribe((req) => {
      emitted = req;
    });

    component.open(null);

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

    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toBeDefined();
    expect(emitted?.firstName).toBe('Anna');
    expect(emitted?.lastName).toBe('Bakker');
    expect(emitted?.email).toBeNull();
  });

  it('should emit cancelled on Annuleren click', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    component.open(null);

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
