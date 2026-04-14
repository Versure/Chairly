import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { NewsletterStore } from '../../data-access';
import { NewsletterCampaignDetail, NewsletterStatus } from '../../models';
import {
  NewsletterEditorComponent,
  NewsletterPreviewModalComponent,
  ScheduleNewsletterDialogComponent,
} from '../../ui';

function stripHtml(html: string): string {
  // Remove tags character-by-character to avoid catastrophic backtracking patterns.
  let result = '';
  let inTag = false;
  for (const ch of html) {
    if (ch === '<') {
      inTag = true;
      continue;
    }
    if (ch === '>') {
      inTag = false;
      continue;
    }
    if (!inTag) {
      result += ch;
    }
  }
  return result.trim();
}

function nonEmptyHtmlValidator(control: FormControl<string>): { [key: string]: boolean } | null {
  return stripHtml(control.value ?? '').length > 0 ? null : { emptyBody: true };
}

@Component({
  selector: 'chairly-newsletter-edit-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NewsletterEditorComponent,
    NewsletterPreviewModalComponent,
    ScheduleNewsletterDialogComponent,
  ],
  templateUrl: './newsletter-edit-page.component.html',
})
export class NewsletterEditPageComponent implements OnInit {
  private readonly store = inject(NewsletterStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);

  private readonly previewModal = viewChild<NewsletterPreviewModalComponent>('previewModal');
  private readonly scheduleDialog = viewChild<ScheduleNewsletterDialogComponent>('scheduleDialog');
  private readonly sendDialogRef = viewChild<ElementRef<HTMLDialogElement>>('sendDialog');

  protected readonly campaignId = signal<string | null>(null);
  protected readonly selectedCampaign = computed<NewsletterCampaignDetail | null>(() =>
    this.store.selectedCampaign(),
  );
  protected readonly isSaving = computed<boolean>(() => this.store.isSaving());
  protected readonly isSending = computed<boolean>(() => this.store.isSending());
  protected readonly error = computed<string | null>(() => this.store.error());
  protected readonly successMessage = computed<string | null>(() => this.store.successMessage());
  protected readonly preview = computed(() => this.store.preview());
  protected readonly isLoadingPreview = computed<boolean>(() => this.store.isLoadingPreview());

  protected readonly status = computed<NewsletterStatus | null>(
    () => this.selectedCampaign()?.status ?? null,
  );
  protected readonly isReadOnly = computed<boolean>(() => {
    const s = this.status();
    return s === 'Sent' || s === 'Sending' || s === 'Cancelled';
  });
  protected readonly isEditMode = computed<boolean>(() => this.campaignId() !== null);
  protected readonly headerTitle = computed<string>(() =>
    this.isEditMode() ? 'Nieuwsbrief bewerken' : 'Nieuwe nieuwsbrief',
  );

  protected readonly form = new FormGroup({
    subject: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(500)],
    }),
    bodyHtml: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, nonEmptyHtmlValidator],
    }),
  });

  constructor() {
    // When selectedCampaign changes (load/create/update), hydrate the form and navigate on create.
    effect(() => {
      const campaign = this.selectedCampaign();
      if (!campaign) return;
      const currentId = this.campaignId();
      if (campaign.id !== currentId) {
        // Just created — navigate to the edit route so subsequent saves are updates.
        this.campaignId.set(campaign.id);
        void this.router.navigate(['/nieuwsbrief', campaign.id, 'bewerken'], {
          replaceUrl: true,
        });
      }
      this.form.patchValue(
        { subject: campaign.subject, bodyHtml: campaign.bodyHtml },
        { emitEvent: false },
      );
      if (this.isReadOnly()) {
        this.form.disable({ emitEvent: false });
      } else {
        this.form.enable({ emitEvent: false });
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.campaignId.set(id);
      this.store.loadCampaign(id);
    }
  }

  protected onBodyChanged(html: string): void {
    this.form.controls.bodyHtml.setValue(html);
    this.form.controls.bodyHtml.markAsDirty();
  }

  protected onSaveDraft(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const request = {
      subject: this.form.controls.subject.value,
      bodyHtml: this.form.controls.bodyHtml.value,
    };
    const id = this.campaignId();
    if (id) {
      this.store.updateCampaign(id, request);
    } else {
      this.store.createCampaign(request);
    }
  }

  protected onPreview(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.store.loadPreview({
      subject: this.form.controls.subject.value,
      bodyHtml: this.form.controls.bodyHtml.value,
    });
    // Use an effect-ish delay: open as soon as preview is set. Simpler: open then rely on template to render.
    queueMicrotask(() => this.previewModal()?.open());
  }

  protected onTestSend(): void {
    const id = this.campaignId();
    if (!id) return;
    this.store.testSendCampaign(id);
  }

  protected onOpenSchedule(): void {
    this.scheduleDialog()?.open();
  }

  protected onScheduleConfirmed(scheduledAtUtc: string): void {
    const id = this.campaignId();
    if (!id) return;
    this.store.scheduleCampaign(id, { scheduledAtUtc });
  }

  protected onOpenSendConfirm(): void {
    const dialog = this.sendDialogRef()?.nativeElement;
    if (dialog && !dialog.open) {
      dialog.showModal();
      this.document.body.style.overflow = 'hidden';
    }
  }

  protected onCloseSendConfirm(): void {
    const dialog = this.sendDialogRef()?.nativeElement;
    if (dialog?.open) {
      dialog.close();
    }
    this.document.body.style.overflow = '';
  }

  protected onConfirmSend(): void {
    this.onCloseSendConfirm();
    const id = this.campaignId();
    if (!id) return;
    this.store.sendCampaign(id);
  }
}
