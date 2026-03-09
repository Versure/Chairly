import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmationDialogComponent } from './confirmation-dialog.component';

describe('ConfirmationDialogComponent', () => {
  let component: ConfirmationDialogComponent;
  let fixture: ComponentFixture<ConfirmationDialogComponent>;
  let dialogEl: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmationDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmationDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('title', 'Delete Item');
    fixture.componentRef.setInput('message', 'Are you sure you want to delete this item?');
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
    component.close();

    expect(dialogEl.close).toHaveBeenCalled();
  });

  it('should emit confirmed and close dialog when confirm button is clicked', () => {
    let confirmed = false;
    component.confirmed.subscribe(() => {
      confirmed = true;
    });

    component.open();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = buttons[1];
    confirmButton.click();
    fixture.detectChanges();

    expect(dialogEl.close).toHaveBeenCalled();
    expect(confirmed).toBe(true);
  });

  it('should emit cancelled and close dialog when cancel button is clicked', () => {
    let cancelled = false;
    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    component.open();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const cancelButton = buttons[0];
    cancelButton.click();
    fixture.detectChanges();

    expect(dialogEl.close).toHaveBeenCalled();
    expect(cancelled).toBe(true);
  });

  it('should use default labels when not provided', () => {
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    expect(buttons[0].textContent?.trim()).toBe('Annuleren');
    expect(buttons[1].textContent?.trim()).toBe('Bevestigen');
  });

  it('should use custom labels when provided', () => {
    fixture.componentRef.setInput('confirmLabel', 'Yes, delete');
    fixture.componentRef.setInput('cancelLabel', 'No, keep it');
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    expect(buttons[0].textContent?.trim()).toBe('No, keep it');
    expect(buttons[1].textContent?.trim()).toBe('Yes, delete');
  });

  it('should apply destructive styling when isDestructive is true', () => {
    fixture.componentRef.setInput('isDestructive', true);
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = buttons[1];
    expect(confirmButton.className).toContain('bg-red-600');
  });

  it('should apply default styling when isDestructive is false', () => {
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const confirmButton = buttons[1];
    expect(confirmButton.className).toContain('bg-primary-600');
  });
});
