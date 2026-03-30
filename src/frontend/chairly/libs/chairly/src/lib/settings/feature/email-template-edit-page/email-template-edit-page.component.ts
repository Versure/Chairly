import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  ConfirmationDialogComponent,
  LoadingIndicatorComponent,
  TemplateTypeLabelPipe,
} from '@org/shared-lib';

import { EmailTemplateApiService, EmailTemplateStore } from '../../data-access';
import { EmailTemplateResponse } from '../../models';
import { EmailPreviewModalComponent } from '../../ui';

@Component({
  selector: 'chairly-email-template-edit-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ConfirmationDialogComponent,
    EmailPreviewModalComponent,
    LoadingIndicatorComponent,
  ],
  providers: [EmailTemplateStore, EmailTemplateApiService],
  templateUrl: './email-template-edit-page.component.html',
})
export class EmailTemplateEditPageComponent implements OnInit {
  private readonly store = inject(EmailTemplateStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly previewModalRef = viewChild.required<EmailPreviewModalComponent>('previewModal');
  private readonly resetDialogRef = viewChild.required<ConfirmationDialogComponent>('resetDialog');

  protected readonly templateType = signal<string>('');
  protected readonly template = computed<EmailTemplateResponse | undefined>(() => {
    const type = this.templateType();
    return this.store.templatesByType()[type];
  });
  protected readonly isLoading = computed<boolean>(() => this.store.isLoading());
  protected readonly isSaving = computed<boolean>(() => this.store.isSaving());
  protected readonly saveSuccess = computed<boolean>(() => this.store.saveSuccess());
  protected readonly saveError = computed<string | null>(() => this.store.saveError());
  protected readonly preview = computed(() => this.store.preview());
  protected readonly isLoadingPreview = computed<boolean>(() => this.store.isLoadingPreview());

  private readonly templateTypeLabelPipe = new TemplateTypeLabelPipe();
  protected readonly pageTitle = computed<string>(() => {
    const type = this.templateType();
    if (!type) return 'Template bewerken';
    return `${this.templateTypeLabelPipe.transform(type)} bewerken`;
  });

  protected readonly form = new FormGroup({
    subject: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(500)],
    }),
    mainMessage: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2000)],
    }),
    closingMessage: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(1000)],
    }),
  });

  constructor() {
    // When the template is loaded, populate the form
    effect(() => {
      const tmpl = this.template();
      if (tmpl) {
        this.form.patchValue({
          subject: tmpl.subject,
          mainMessage: tmpl.mainMessage,
          closingMessage: tmpl.closingMessage,
        });
      }
    });

    // When preview result arrives, open the modal
    effect(() => {
      const p = this.preview();
      if (p) {
        this.previewModalRef().open();
      }
    });
  }

  ngOnInit(): void {
    const type = this.route.snapshot.paramMap.get('templateType') ?? '';
    this.templateType.set(type);
    this.store.loadTemplates();
  }

  protected onSave(): void {
    if (this.form.invalid || this.isSaving()) {
      return;
    }
    this.store.updateTemplate(this.templateType(), {
      subject: this.form.value.subject ?? '',
      mainMessage: this.form.value.mainMessage ?? '',
      closingMessage: this.form.value.closingMessage ?? '',
    });
  }

  protected onPreview(): void {
    this.store.previewTemplate({
      templateType: this.templateType(),
      subject: this.form.value.subject ?? '',
      mainMessage: this.form.value.mainMessage ?? '',
      closingMessage: this.form.value.closingMessage ?? '',
    });
  }

  protected onClosePreview(): void {
    this.store.clearPreview();
  }

  protected onResetRequest(): void {
    this.resetDialogRef().open();
  }

  protected onConfirmReset(): void {
    this.store.resetTemplate(this.templateType());
    void this.router.navigate(['/instellingen/email-templates']);
  }
}
