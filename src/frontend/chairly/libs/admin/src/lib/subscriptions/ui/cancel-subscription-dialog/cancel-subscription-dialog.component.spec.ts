import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CancelSubscriptionDialogComponent } from './cancel-subscription-dialog.component';

describe('CancelSubscriptionDialogComponent', () => {
  let component: CancelSubscriptionDialogComponent;
  let fixture: ComponentFixture<CancelSubscriptionDialogComponent>;

  beforeEach(async () => {
    // Mock dialog methods not available in JSDOM
    HTMLDialogElement.prototype.showModal = vi.fn();
    HTMLDialogElement.prototype.close = vi.fn();

    await TestBed.configureTestingModule({
      imports: [CancelSubscriptionDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CancelSubscriptionDialogComponent);
    fixture.componentRef.setInput('isSubmitting', false);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render dialog title', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Abonnement annuleren');
  });

  it('should render the reason field', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('textarea')).toBeTruthy();
    expect(compiled.textContent).toContain('Reden');
  });

  it('should have reason control required', () => {
    expect(component['reasonControl'].valid).toBe(false);
    component['reasonControl'].setValue('Test reden');
    expect(component['reasonControl'].valid).toBe(true);
  });

  it('should emit confirm with reason when valid', () => {
    const confirmSpy = vi.fn();
    component.confirm.subscribe(confirmSpy);

    component['reasonControl'].setValue('Niet betaald');
    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    // The last button is the confirm/submit button
    const confirmButton = buttons[buttons.length - 1];
    confirmButton?.click();

    expect(confirmSpy).toHaveBeenCalledWith('Niet betaald');
  });

  it('should emit cancelled on cancel click', () => {
    const cancelSpy = vi.fn();
    component.cancelled.subscribe(cancelSpy);

    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    // The first button is the cancel button
    buttons[0]?.click();

    expect(cancelSpy).toHaveBeenCalled();
  });

  it('should not emit confirm when reason is empty', () => {
    const confirmSpy = vi.fn();
    component.confirm.subscribe(confirmSpy);

    component['reasonControl'].setValue('');
    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = buttons[buttons.length - 1];
    confirmButton?.click();

    expect(confirmSpy).not.toHaveBeenCalled();
  });
});
