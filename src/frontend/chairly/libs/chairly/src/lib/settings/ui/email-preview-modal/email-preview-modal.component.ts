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
  selector: 'chairly-email-preview-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './email-preview-modal.component.html',
  styleUrl: './email-preview-modal.component.scss',
})
export class EmailPreviewModalComponent {
  readonly subject = input<string>('');
  readonly htmlBody = input<string>('');

  private readonly document = inject(DOCUMENT);
  private readonly dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  open(): void {
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }
}
