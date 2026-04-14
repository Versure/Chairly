import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';

import { LoadingIndicatorComponent, PageHeaderComponent } from '@org/shared-lib';

import { NewsletterStore } from '../../data-access';
import { NewsletterCampaignSummary } from '../../models';
import { NewsletterStatusLabelPipe } from '../../pipes';

@Component({
  selector: 'chairly-newsletter-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LoadingIndicatorComponent, NewsletterStatusLabelPipe, PageHeaderComponent],
  templateUrl: './newsletter-list-page.component.html',
})
export class NewsletterListPageComponent implements OnInit {
  private readonly store = inject(NewsletterStore);
  private readonly router = inject(Router);

  protected readonly campaigns = computed<NewsletterCampaignSummary[]>(() =>
    this.store.campaigns(),
  );
  protected readonly isLoading = computed<boolean>(() => this.store.isLoadingList());
  protected readonly error = computed<string | null>(() => this.store.error());

  ngOnInit(): void {
    this.store.loadCampaigns();
  }

  protected onCreateNew(): void {
    void this.router.navigate(['/nieuwsbrief/nieuw']);
  }

  protected onOpen(campaign: NewsletterCampaignSummary): void {
    if (campaign.status === 'Draft') {
      void this.router.navigate(['/nieuwsbrief', campaign.id, 'bewerken']);
    } else {
      void this.router.navigate(['/nieuwsbrief', campaign.id]);
    }
  }
}
