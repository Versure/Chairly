import { DatePipe } from '@angular/common';
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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ConfirmationDialogComponent, LoadingIndicatorComponent } from '@org/shared-lib';

import { NewsletterStore } from '../../data-access';
import { NewsletterCampaignDetail } from '../../models';
import { NewsletterStatusLabelPipe } from '../../pipes';

type PendingAction = 'cancel' | 'delete' | null;

@Component({
  selector: 'chairly-newsletter-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    ConfirmationDialogComponent,
    LoadingIndicatorComponent,
    NewsletterStatusLabelPipe,
  ],
  templateUrl: './newsletter-detail-page.component.html',
})
export class NewsletterDetailPageComponent implements OnInit {
  private readonly store = inject(NewsletterStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly confirmDialog = viewChild<ConfirmationDialogComponent>('confirmDialog');

  protected readonly campaign = computed<NewsletterCampaignDetail | null>(() =>
    this.store.selectedCampaign(),
  );
  protected readonly isLoading = computed<boolean>(() => this.store.isLoadingDetail());
  protected readonly error = computed<string | null>(() => this.store.error());
  protected readonly preview = computed(() => this.store.preview());

  protected readonly pendingAction = signal<PendingAction>(null);
  protected readonly confirmTitle = computed<string>(() =>
    this.pendingAction() === 'delete' ? 'Nieuwsbrief verwijderen' : 'Nieuwsbrief annuleren',
  );
  protected readonly confirmMessage = computed<string>(() =>
    this.pendingAction() === 'delete'
      ? 'Weet u zeker dat u deze nieuwsbrief wilt verwijderen?'
      : 'Weet u zeker dat u deze geplande nieuwsbrief wilt annuleren?',
  );

  constructor() {
    effect(() => {
      const c = this.campaign();
      if (c && c.bodyHtml) {
        this.store.loadPreview({ subject: c.subject, bodyHtml: c.bodyHtml });
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.store.loadCampaign(id);
    }
  }

  protected onEdit(): void {
    const id = this.campaign()?.id;
    if (id) {
      void this.router.navigate(['/nieuwsbrief', id, 'bewerken']);
    }
  }

  protected onRequestCancel(): void {
    this.pendingAction.set('cancel');
    this.confirmDialog()?.open();
  }

  protected onRequestDelete(): void {
    this.pendingAction.set('delete');
    this.confirmDialog()?.open();
  }

  protected onConfirmAction(): void {
    const id = this.campaign()?.id;
    if (!id) return;
    const action = this.pendingAction();
    this.pendingAction.set(null);
    if (action === 'cancel') {
      this.store.cancelCampaign(id);
    } else if (action === 'delete') {
      this.store.deleteCampaign(id);
      void this.router.navigate(['/nieuwsbrief']);
    }
  }

  protected onCancelAction(): void {
    this.pendingAction.set(null);
  }
}
