import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import {
  CreateNewsletterCampaignRequest,
  NewsletterCampaignDetail,
  NewsletterCampaignSummary,
  PreviewNewsletterRequest,
  PreviewNewsletterResponse,
  ScheduleNewsletterCampaignRequest,
  UpdateNewsletterCampaignRequest,
} from '../models';
import { NewslettersApiService } from './newsletters-api.service';

export interface NewsletterState {
  campaigns: NewsletterCampaignSummary[];
  selectedCampaign: NewsletterCampaignDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  isSaving: boolean;
  isSending: boolean;
  error: string | null;
  successMessage: string | null;
  preview: PreviewNewsletterResponse | null;
  isLoadingPreview: boolean;
}

const initialState: NewsletterState = {
  campaigns: [],
  selectedCampaign: null,
  isLoadingList: false,
  isLoadingDetail: false,
  isSaving: false,
  isSending: false,
  error: null,
  successMessage: null,
  preview: null,
  isLoadingPreview: false,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

function detailToSummary(detail: NewsletterCampaignDetail): NewsletterCampaignSummary {
  return {
    id: detail.id,
    subject: detail.subject,
    status: detail.status,
    recipientCount: detail.totalRecipients,
    sentCount: detail.sentCount,
    failedCount: detail.failedCount,
    scheduledAtUtc: detail.scheduledAtUtc ?? null,
    sentAtUtc: detail.sentAtUtc ?? null,
    createdAtUtc: detail.createdAtUtc,
  };
}

function upsertCampaign(
  campaigns: NewsletterCampaignSummary[],
  detail: NewsletterCampaignDetail,
): NewsletterCampaignSummary[] {
  const summary = detailToSummary(detail);
  const index = campaigns.findIndex((c) => c.id === detail.id);
  if (index === -1) {
    return [summary, ...campaigns];
  }
  const next = campaigns.slice();
  next[index] = summary;
  return next;
}

function removeCampaign(
  campaigns: NewsletterCampaignSummary[],
  id: string,
): NewsletterCampaignSummary[] {
  return campaigns.filter((c) => c.id !== id);
}

export const NewsletterStore = signalStore(
  withState<NewsletterState>(initialState),
  withComputed((store) => ({
    draftCampaigns: computed(() => store.campaigns().filter((c) => c.status === 'Draft')),
    scheduledCampaigns: computed(() => store.campaigns().filter((c) => c.status === 'Scheduled')),
    sentCampaigns: computed(() => store.campaigns().filter((c) => c.status === 'Sent')),
    cancelledCampaigns: computed(() => store.campaigns().filter((c) => c.status === 'Cancelled')),
  })),
  withMethods((store) => {
    const api = inject(NewslettersApiService);

    return {
      loadCampaigns(): void {
        patchState(store, { isLoadingList: true, error: null });
        api
          .listCampaigns()
          .pipe(take(1))
          .subscribe({
            next: (campaigns) => patchState(store, { campaigns, isLoadingList: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoadingList: false }),
          });
      },

      loadCampaign(id: string): void {
        patchState(store, { isLoadingDetail: true, error: null });
        api
          .getCampaign(id)
          .pipe(take(1))
          .subscribe({
            next: (selectedCampaign) =>
              patchState(store, { selectedCampaign, isLoadingDetail: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoadingDetail: false }),
          });
      },

      createCampaign(request: CreateNewsletterCampaignRequest): void {
        patchState(store, { isSaving: true, error: null });
        api
          .createCampaign(request)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, (state) => ({
                selectedCampaign: detail,
                campaigns: upsertCampaign(state.campaigns, detail),
                isSaving: false,
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isSaving: false }),
          });
      },

      updateCampaign(id: string, request: UpdateNewsletterCampaignRequest): void {
        patchState(store, { isSaving: true, error: null });
        api
          .updateCampaign(id, request)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, (state) => ({
                selectedCampaign: detail,
                campaigns: upsertCampaign(state.campaigns, detail),
                isSaving: false,
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isSaving: false }),
          });
      },

      deleteCampaign(id: string): void {
        patchState(store, { error: null });
        api
          .deleteCampaign(id)
          .pipe(take(1))
          .subscribe({
            next: () =>
              patchState(store, (state) => ({
                campaigns: removeCampaign(state.campaigns, id),
                selectedCampaign: state.selectedCampaign?.id === id ? null : state.selectedCampaign,
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      scheduleCampaign(id: string, request: ScheduleNewsletterCampaignRequest): void {
        patchState(store, { isSaving: true, error: null });
        api
          .scheduleCampaign(id, request)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, (state) => ({
                selectedCampaign: detail,
                campaigns: upsertCampaign(state.campaigns, detail),
                isSaving: false,
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isSaving: false }),
          });
      },

      cancelCampaign(id: string): void {
        patchState(store, { error: null });
        api
          .cancelCampaign(id)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, (state) => ({
                selectedCampaign: detail,
                campaigns: upsertCampaign(state.campaigns, detail),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      sendCampaign(id: string): void {
        patchState(store, { isSending: true, error: null });
        api
          .sendCampaign(id)
          .pipe(take(1))
          .subscribe({
            next: (detail) =>
              patchState(store, (state) => ({
                selectedCampaign: detail,
                campaigns: upsertCampaign(state.campaigns, detail),
                isSending: false,
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isSending: false }),
          });
      },

      testSendCampaign(id: string): void {
        patchState(store, { error: null, successMessage: null });
        api
          .testSendCampaign(id)
          .pipe(take(1))
          .subscribe({
            next: () =>
              patchState(store, {
                successMessage: 'Test-e-mail verzonden naar uw eigen adres.',
              }),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      clearSuccessMessage(): void {
        patchState(store, { successMessage: null });
      },

      loadPreview(request: PreviewNewsletterRequest): void {
        patchState(store, { isLoadingPreview: true, error: null });
        api
          .previewNewsletter(request)
          .pipe(take(1))
          .subscribe({
            next: (preview) => patchState(store, { preview, isLoadingPreview: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoadingPreview: false }),
          });
      },

      clearPreview(): void {
        patchState(store, { preview: null });
      },
    };
  }),
);
