import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProvisionSubscriptionDialogComponent } from './provision-subscription-dialog.component';

describe('ProvisionSubscriptionDialogComponent', () => {
  let component: ProvisionSubscriptionDialogComponent;
  let fixture: ComponentFixture<ProvisionSubscriptionDialogComponent>;

  beforeEach(async () => {
    // Mock dialog methods not available in JSDOM
    HTMLDialogElement.prototype.showModal = vi.fn();
    HTMLDialogElement.prototype.close = vi.fn();

    await TestBed.configureTestingModule({
      imports: [ProvisionSubscriptionDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ProvisionSubscriptionDialogComponent);
    fixture.componentRef.setInput('salonName', 'Salon Test');
    fixture.componentRef.setInput('isSubmitting', false);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the salon name', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Salon Test');
  });

  it('should render dialog title', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Abonnement activeren');
  });

  it('should emit confirm on confirm click', () => {
    const confirmSpy = vi.fn();
    component.confirm.subscribe(confirmSpy);

    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = Array.from(buttons).find((b) => b.textContent?.trim() === 'Activeren');
    confirmButton?.click();

    expect(confirmSpy).toHaveBeenCalled();
  });

  it('should emit cancelled on cancel click', () => {
    const cancelSpy = vi.fn();
    component.cancelled.subscribe(cancelSpy);

    component.open();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const cancelButton = Array.from(buttons).find((b) => b.textContent?.trim() === 'Annuleren');
    cancelButton?.click();

    expect(cancelSpy).toHaveBeenCalled();
  });
});
