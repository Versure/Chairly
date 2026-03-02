import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateServiceRequest, ServiceCategoryResponse, ServiceResponse } from '../util';
import { ServiceFormDialogComponent } from './service-form-dialog.component';

const mockCategories: ServiceCategoryResponse[] = [
  { id: 'cat-1', name: 'Hair', sortOrder: 1, createdAtUtc: '2026-01-01T00:00:00Z', createdBy: 'user' },
];

const mockService: ServiceResponse = {
  id: 'svc-1',
  name: "Men's Haircut",
  description: 'A classic cut',
  duration: '01:30:00',
  price: 25,
  categoryId: 'cat-1',
  categoryName: 'Hair',
  isActive: true,
  sortOrder: 1,
  createdAtUtc: '2026-01-01T00:00:00Z',
  createdBy: 'user',
  updatedAtUtc: null,
  updatedBy: null,
};

describe('ServiceFormDialogComponent', () => {
  let component: ServiceFormDialogComponent;
  let fixture: ComponentFixture<ServiceFormDialogComponent>;
  let dialogEl: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceFormDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceFormDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('categories', mockCategories);
    fixture.detectChanges();

    dialogEl = fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    dialogEl.showModal = vi.fn();
    dialogEl.close = vi.fn();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should open the dialog when open() is called', () => {
    component.open();

    expect(dialogEl.showModal).toHaveBeenCalled();
  });

  it('should close the dialog when close() is called', () => {
    component.open();
    component.close();

    expect(dialogEl.close).toHaveBeenCalled();
  });

  it('should reset form to defaults when opened in create mode', () => {
    component.open();
    fixture.detectChanges();

    const nameInput = fixture.nativeElement.querySelector(
      'input[formControlName="name"]',
    ) as HTMLInputElement;
    expect(nameInput.value).toBe('');
  });

  it('should pre-populate form with service values when opened in edit mode', () => {
    fixture.componentRef.setInput('service', mockService);
    fixture.detectChanges();
    component.open();
    fixture.detectChanges();

    const nameInput = fixture.nativeElement.querySelector(
      'input[formControlName="name"]',
    ) as HTMLInputElement;
    const durationInput = fixture.nativeElement.querySelector(
      'input[formControlName="duration"]',
    ) as HTMLInputElement;
    expect(nameInput.value).toBe("Men's Haircut");
    expect(durationInput.value).toBe('90');
  });

  it('should emit saved event with TimeSpan-converted duration on form submit', () => {
    let emitted: CreateServiceRequest | undefined;
    component.saved.subscribe((req) => {
      emitted = req as CreateServiceRequest;
    });

    component.open();

    const nameInput = fixture.nativeElement.querySelector(
      'input[formControlName="name"]',
    ) as HTMLInputElement;
    nameInput.value = 'Test Service';
    nameInput.dispatchEvent(new Event('input'));

    const durationInput = fixture.nativeElement.querySelector(
      'input[formControlName="duration"]',
    ) as HTMLInputElement;
    durationInput.value = '60';
    durationInput.dispatchEvent(new Event('input'));

    const priceInput = fixture.nativeElement.querySelector(
      'input[formControlName="price"]',
    ) as HTMLInputElement;
    priceInput.value = '20';
    priceInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toBeDefined();
    expect(emitted?.name).toBe('Test Service');
    expect(emitted?.duration).toBe('01:00:00');
    expect(emitted?.price).toBe(20);
  });

  it('should not emit saved when name field is empty', () => {
    let emitted = false;
    component.saved.subscribe(() => {
      emitted = true;
    });

    component.open();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(emitted).toBe(false);
  });

  it('should emit cancelled and close dialog when cancel button is clicked', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    component.open();

    const cancelButton = fixture.nativeElement.querySelector(
      'button[type="button"]',
    ) as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    expect(cancelled).toBe(true);
    expect(dialogEl.close).toHaveBeenCalled();
  });

  it('should show validation message when name is invalid and touched', () => {
    component.open();

    const nameInput = fixture.nativeElement.querySelector(
      'input[formControlName="name"]',
    ) as HTMLInputElement;
    nameInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const errorMsg = fixture.nativeElement.querySelector(
      'p.text-red-600',
    ) as HTMLParagraphElement | null;
    expect(errorMsg).toBeTruthy();
  });

  it('should display category options from input', () => {
    component.open();
    fixture.detectChanges();

    const options = fixture.nativeElement.querySelectorAll(
      'select option',
    ) as NodeListOf<HTMLOptionElement>;
    expect(options.length).toBe(2);
    expect(options[1].textContent?.trim()).toBe('Hair');
  });
});
