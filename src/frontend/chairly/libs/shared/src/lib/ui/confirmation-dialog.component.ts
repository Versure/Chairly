import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
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
  readonly confirmLabel = input<string>('Confirm');
  readonly cancelLabel = input<string>('Cancel');
  readonly isDestructive = input<boolean>(false);

  readonly confirmed: OutputEmitterRef<void> = output<void>();
  readonly cancelled: OutputEmitterRef<void> = output<void>();

  private readonly dialogRef =
    viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  protected readonly confirmButtonClass = computed<string>(() => {
    const base =
      'px-4 py-2 text-sm font-medium text-white rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2';
    return this.isDestructive()
      ? `${base} bg-red-600 hover:bg-red-700 focus:ring-red-500`
      : `${base} bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500`;
  });

  open(): void {
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
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
