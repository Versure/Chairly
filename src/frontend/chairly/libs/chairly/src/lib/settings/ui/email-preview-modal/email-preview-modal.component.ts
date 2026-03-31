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
  private readonly iframeRef = viewChild.required<ElementRef<HTMLIFrameElement>>('previewIframe');

  open(): void {
    this.document.body.style.overflow = 'hidden';
    this.dialogRef().nativeElement.showModal();
    this.writeIframeContent();
  }

  close(): void {
    this.document.body.style.overflow = '';
    this.dialogRef().nativeElement.close();
  }

  private writeIframeContent(): void {
    const iframe = this.iframeRef().nativeElement;
    iframe.srcdoc = this.htmlBody();
  }
}
