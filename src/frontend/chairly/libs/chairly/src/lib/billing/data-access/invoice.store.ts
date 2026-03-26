import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { PaymentMethod } from '@org/shared-lib';

import {
  AddLineItemRequest,
  CompanyInfo,
  Invoice,
  InvoiceFilterParams,
  InvoiceSummary,
} from '../models';
import { InvoiceApiService } from './invoice-api.service';

export interface InvoiceState {
  invoices: InvoiceSummary[];
  selectedInvoice: Invoice | null;
  companyInfo: CompanyInfo | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: InvoiceState = {
  invoices: [],
  selectedInvoice: null,
  companyInfo: null,
  isLoading: false,
  error: null,
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

    if (
      typeof err.error === 'object' &&
      err.error !== null &&
      'detail' in err.error &&
      typeof err.error.detail === 'string' &&
      err.error.detail.trim().length > 0
    ) {
      return err.error.detail;
    }

    if (
      typeof err.error === 'object' &&
      err.error !== null &&
      'title' in err.error &&
      typeof err.error.title === 'string' &&
      err.error.title.trim().length > 0
    ) {
      return err.error.title;
    }
  }

  return err instanceof Error ? err.message : String(err);
}

function replaceInvoiceSummary(invoices: InvoiceSummary[], updated: Invoice): InvoiceSummary[] {
  return invoices.map((item) =>
    item.id === updated.id
      ? {
          id: updated.id,
          invoiceNumber: updated.invoiceNumber,
          invoiceDate: updated.invoiceDate,
          bookingId: updated.bookingId,
          clientId: updated.clientId,
          clientFullName: updated.clientFullName,
          subTotalAmount: updated.subTotalAmount,
          totalVatAmount: updated.totalVatAmount,
          totalAmount: updated.totalAmount,
          paymentMethod: updated.paymentMethod,
          status: updated.status,
          createdAtUtc: updated.createdAtUtc,
          sentAtUtc: updated.sentAtUtc,
          paidAtUtc: updated.paidAtUtc,
          voidedAtUtc: updated.voidedAtUtc,
        }
      : item,
  );
}

export const InvoiceStore = signalStore(
  withState<InvoiceState>(initialState),
  withMethods((store) => {
    const invoiceApi = inject(InvoiceApiService);

    return {
      loadInvoices(filters?: InvoiceFilterParams): void {
        patchState(store, { isLoading: true, error: null });
        invoiceApi
          .getInvoices(filters)
          .pipe(take(1))
          .subscribe({
            next: (invoices) => patchState(store, { invoices, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      loadInvoice(id: string): void {
        patchState(store, { isLoading: true, error: null });
        invoiceApi
          .getInvoice(id)
          .pipe(take(1))
          .subscribe({
            next: (invoice) => patchState(store, { selectedInvoice: invoice, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      sendInvoice(id: string): void {
        patchState(store, { error: null });
        invoiceApi
          .sendInvoice(id)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      markAsPaid(id: string, paymentMethod: PaymentMethod): void {
        invoiceApi
          .markAsPaid(id, paymentMethod)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      voidInvoice(id: string): void {
        invoiceApi
          .voidInvoice(id)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      addLineItem(invoiceId: string, lineItem: AddLineItemRequest): void {
        invoiceApi
          .addLineItem(invoiceId, lineItem)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      removeLineItem(invoiceId: string, lineItemId: string): void {
        invoiceApi
          .removeLineItem(invoiceId, lineItemId)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
              })),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },

      regenerateInvoice(id: string): void {
        patchState(store, { isLoading: true, error: null });
        invoiceApi
          .regenerateInvoice(id)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                selectedInvoice: updated,
                invoices: replaceInvoiceSummary(state.invoices, updated),
                isLoading: false,
              })),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      loadCompanyInfo(): void {
        invoiceApi
          .getCompanyInfo()
          .pipe(take(1))
          .subscribe({
            next: (companyInfo) => patchState(store, { companyInfo }),
            error: (err: unknown) => patchState(store, { error: toErrorMessage(err) }),
          });
      },
    };
  }),
);
