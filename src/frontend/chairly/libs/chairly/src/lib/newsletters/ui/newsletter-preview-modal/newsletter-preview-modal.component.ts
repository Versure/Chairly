import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'chairly-newsletter-preview-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './newsletter-preview-modal.component.html',
})
export class NewsletterPreviewModalComponent {
  readonly subject = input<string>('');
  readonly htmlBody = input<string>('');

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  open(): void {
    const dialog = this.dialogRef().nativeElement;
    if (!dialog.open) {
      dialog.showModal();
      this.document.body.style.overflow = 'hidden';
    }
  }

  close(): void {
    const dialog = this.dialogRef().nativeElement;
    if (dialog.open) {
      dialog.close();
    }
    this.document.body.style.overflow = '';
  }
}
