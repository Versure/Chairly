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
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'chairly-admin-cancel-subscription-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './cancel-subscription-dialog.component.html',
  styleUrl: './cancel-subscription-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CancelSubscriptionDialogComponent {
  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly isSubmitting = input<boolean>(false);

  readonly confirm = output<string>();
  readonly cancelled = output<void>();

  protected readonly reasonControl = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(1000)],
  });

  open(): void {
    this.dialogRef().nativeElement.showModal();
    this.document.body.style.overflow = 'hidden';
  }

  close(): void {
    this.dialogRef().nativeElement.close();
    this.reasonControl.reset();
    this.document.body.style.overflow = '';
  }

  protected onConfirm(): void {
    if (this.reasonControl.valid) {
      this.confirm.emit(this.reasonControl.value);
    }
  }

  protected onCancel(): void {
    this.cancelled.emit();
    this.close();
  }
}
