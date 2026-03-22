import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'chairly-admin-provision-subscription-dialog',
  standalone: true,
  templateUrl: './provision-subscription-dialog.component.html',
  styleUrl: './provision-subscription-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProvisionSubscriptionDialogComponent {
  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly salonName = input.required<string>();
  readonly isSubmitting = input<boolean>(false);

  readonly confirm = output<void>();
  readonly cancelled = output<void>();

  open(): void {
    this.dialogRef().nativeElement.showModal();
    this.document.body.style.overflow = 'hidden';
  }

  close(): void {
    this.dialogRef().nativeElement.close();
    this.document.body.style.overflow = '';
  }

  protected onConfirm(): void {
    this.confirm.emit();
  }

  protected onCancel(): void {
    this.cancelled.emit();
    this.close();
  }
}
