import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { switchMap, take } from 'rxjs';

import {
  EmailTemplateResponse,
  PreviewEmailTemplateRequest,
  PreviewEmailTemplateResponse,
  UpdateEmailTemplateRequest,
} from '../models';
import { EmailTemplateApiService } from './email-template-api.service';

export interface EmailTemplateState {
  templates: EmailTemplateResponse[];
  isLoading: boolean;
  error: string | null;
  isSaving: boolean;
  saveError: string | null;
  saveSuccess: boolean;
  preview: PreviewEmailTemplateResponse | null;
  isLoadingPreview: boolean;
}

const initialState: EmailTemplateState = {
  templates: [],
  isLoading: false,
  error: null,
  isSaving: false,
  saveError: null,
  saveSuccess: false,
  preview: null,
  isLoadingPreview: false,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

function replaceTemplate(
  templates: EmailTemplateResponse[],
  templateType: string,
  updated: EmailTemplateResponse,
): EmailTemplateResponse[] {
  return templates.map((item) => (item.templateType === templateType ? updated : item));
}

export const EmailTemplateStore = signalStore(
  withState<EmailTemplateState>(initialState),
  withComputed((store) => ({
    templatesByType: computed<Record<string, EmailTemplateResponse>>(() => {
      const map: Record<string, EmailTemplateResponse> = {};
      for (const t of store.templates()) {
        map[t.templateType] = t;
      }
      return map;
    }),
  })),
  withMethods((store) => {
    const emailTemplateApi = inject(EmailTemplateApiService);

    return {
      loadTemplates(): void {
        patchState(store, { isLoading: true, error: null });
        emailTemplateApi
          .getEmailTemplates()
          .pipe(take(1))
          .subscribe({
            next: (templates) => patchState(store, { templates, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      updateTemplate(templateType: string, request: UpdateEmailTemplateRequest): void {
        patchState(store, { isSaving: true, saveError: null, saveSuccess: false });
        emailTemplateApi
          .updateEmailTemplate(templateType, request)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                templates: replaceTemplate(state.templates, templateType, updated),
                isSaving: false,
                saveSuccess: true,
              })),
            error: (err: unknown) =>
              patchState(store, {
                saveError: toErrorMessage(err),
                isSaving: false,
              }),
          });
      },

      resetTemplate(templateType: string): void {
        patchState(store, { isSaving: true, saveError: null, saveSuccess: false });
        emailTemplateApi
          .resetEmailTemplate(templateType)
          .pipe(
            switchMap(() => {
              patchState(store, { isSaving: false, isLoading: true, error: null });
              return emailTemplateApi.getEmailTemplates();
            }),
            take(1),
          )
          .subscribe({
            next: (templates) => patchState(store, { templates, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                saveError: toErrorMessage(err),
                isSaving: false,
                isLoading: false,
              }),
          });
      },

      previewTemplate(request: PreviewEmailTemplateRequest): void {
        patchState(store, { isLoadingPreview: true, preview: null });
        emailTemplateApi
          .previewEmailTemplate(request)
          .pipe(take(1))
          .subscribe({
            next: (preview) => patchState(store, { preview, isLoadingPreview: false }),
            error: () => patchState(store, { isLoadingPreview: false }),
          });
      },

      clearSaveSuccess(): void {
        patchState(store, { saveSuccess: false });
      },

      clearPreview(): void {
        patchState(store, { preview: null });
      },
    };
  }),
);
