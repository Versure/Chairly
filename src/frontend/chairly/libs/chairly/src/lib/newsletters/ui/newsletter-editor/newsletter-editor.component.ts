import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { QuillEditorComponent } from 'ngx-quill';

@Component({
  selector: 'chairly-newsletter-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, QuillEditorComponent],
  templateUrl: './newsletter-editor.component.html',
  styleUrl: './newsletter-editor.component.scss',
})
export class NewsletterEditorComponent {
  readonly value = model<string>('');
  readonly placeholder = input<string>('Schrijf hier uw nieuwsbrief...');

  protected readonly quillModules = {
    toolbar: [
      [{ header: [2, 3, false] }],
      ['bold', 'italic', 'underline'],
      [{ list: 'ordered' }, { list: 'bullet' }],
      ['link'],
      ['clean'],
    ],
  };

  protected onContentChanged(html: string | null): void {
    this.value.set(html ?? '');
  }
}
