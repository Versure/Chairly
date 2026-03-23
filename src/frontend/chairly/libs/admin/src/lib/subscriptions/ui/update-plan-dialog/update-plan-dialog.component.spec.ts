import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UpdatePlanDialogComponent } from './update-plan-dialog.component';

describe('UpdatePlanDialogComponent', () => {
  let component: UpdatePlanDialogComponent;
  let fixture: ComponentFixture<UpdatePlanDialogComponent>;

  beforeEach(async () => {
    // Mock dialog methods not available in JSDOM
    HTMLDialogElement.prototype.showModal = vi.fn();
    HTMLDialogElement.prototype.close = vi.fn();

    await TestBed.configureTestingModule({
      imports: [UpdatePlanDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(UpdatePlanDialogComponent);
    fixture.componentRef.setInput('currentPlan', 'starter');
    fixture.componentRef.setInput('currentBillingCycle', 'Monthly');
    fixture.componentRef.setInput('isSubmitting', false);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render dialog title', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Plan wijzigen');
  });

  it('should render plan and billing cycle selects', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const selects = compiled.querySelectorAll('select');
    expect(selects.length).toBe(2);
  });

  it('should populate form with current values when opened', () => {
    component.open();
    fixture.detectChanges();

    expect(component['form'].value.plan).toBe('starter');
    expect(component['form'].value.billingCycle).toBe('Monthly');
  });

  it('should emit confirm with form values', () => {
    const confirmSpy = vi.fn();
    component.confirm.subscribe(confirmSpy);

    component.open();
    fixture.detectChanges();

    component['form'].patchValue({ plan: 'team', billingCycle: 'Annual' });
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = buttons[buttons.length - 1];
    confirmButton?.click();

    expect(confirmSpy).toHaveBeenCalledWith({ plan: 'team', billingCycle: 'Annual' });
  });

  it('should emit cancelled on cancel click', () => {
    const cancelSpy = vi.fn();
    component.cancelled.subscribe(cancelSpy);

    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    // First button is the cancel button
    buttons[0]?.click();

    expect(cancelSpy).toHaveBeenCalled();
  });

  it('should reset form on close', () => {
    component.open();
    component['form'].patchValue({ plan: 'team', billingCycle: 'Annual' });
    component.close();

    expect(component['form'].value.plan).toBe('');
    expect(component['form'].value.billingCycle).toBeNull();
  });
});
