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
  template: `
    <dialog
      class="p-0 rounded-lg shadow-xl max-w-md w-full"
      #dialogEl
    >
      <div class="p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-2">
          {{ title() }}
        </h2>
        <p class="text-sm text-gray-600 mb-6">{{ message() }}</p>
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            (click)="onCancel()"
          >
            {{ cancelLabel() }}
          </button>
          <button
            type="button"
            [class]="confirmButtonClass()"
            (click)="onConfirm()"
          >
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    </dialog>
  `,
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
