import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { PeriodType, RevenueReport } from '../models';
import { ReportApiService } from './report-api.service';

export interface ReportState {
  report: RevenueReport | null;
  isLoading: boolean;
  isDownloading: boolean;
  error: string | null;
  selectedPeriod: PeriodType;
  selectedDate: string;
}

const initialState: ReportState = {
  report: null,
  isLoading: false,
  isDownloading: false,
  error: null,
  selectedPeriod: 'week',
  selectedDate: new Date().toISOString().split('T')[0],
};

function toErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (typeof err.error === 'string' && err.error.trim().length > 0) {
      return err.error;
    }

    if (
      typeof err.error === 'object' &&
      err.error !== null &&
      'message' in err.error &&
      typeof err.error.message === 'string' &&
      err.error.message.trim().length > 0
    ) {
      return err.error.message;
    }
  }

  return err instanceof Error ? err.message : String(err);
}

export const ReportStore = signalStore(
  withState<ReportState>(initialState),
  withMethods((store) => {
    const reportApi = inject(ReportApiService);
    const doc = inject(DOCUMENT);

    return {
      loadReport(period: PeriodType, date: string): void {
        patchState(store, {
          isLoading: true,
          error: null,
          selectedPeriod: period,
          selectedDate: date,
        });
        reportApi
          .getRevenueReport(period, date)
          .pipe(take(1))
          .subscribe({
            next: (report) => patchState(store, { report, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoading: false }),
          });
      },

      downloadPdf(period: PeriodType, date: string): void {
        patchState(store, { isDownloading: true });
        reportApi
          .downloadRevenueReportPdf(period, date)
          .pipe(take(1))
          .subscribe({
            next: (blob) => {
              const url = URL.createObjectURL(blob);
              const anchor = doc.createElement('a');
              anchor.href = url;
              anchor.download = `omzetrapport-${period}-${date}.pdf`;
              anchor.click();
              URL.revokeObjectURL(url);
              patchState(store, { isDownloading: false });
            },
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isDownloading: false }),
          });
      },
    };
  }),
);
