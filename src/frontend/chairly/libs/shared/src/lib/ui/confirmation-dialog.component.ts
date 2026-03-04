import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
  OutputEmitterRef,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'chairly-confirmation-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './confirmation-dialog.component.html',
})
export class ConfirmationDialogComponent {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input<string>('Bevestigen');
  readonly cancelLabel = input<string>('Annuleren');
  readonly isDestructive = input<boolean>(false);

  readonly confirmed: OutputEmitterRef<void> = output<void>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly confirmButtonClass = computed<string>(() => {
    const base =
      'px-4 py-2 text-sm font-medium text-white rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2';
    return this.isDestructive()
      ? `${base} bg-red-600 hover:bg-red-700 focus:ring-red-500`
      : `${base} bg-primary-600 hover:bg-primary-700 focus:ring-primary-500`;
  });

  open(): void {
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  protected onConfirm(): void {
    this.close();
    this.confirmed.emit();
  }

  protected onCancel(): void {
    this.close();
    this.cancelled.emit();
  }
}
