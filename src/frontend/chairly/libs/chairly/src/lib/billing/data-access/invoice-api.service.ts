import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  AddLineItemRequest,
  CompanyInfo,
  Invoice,
  InvoiceFilterParams,
  InvoiceSummary,
} from '../models';

@Injectable({ providedIn: 'root' })
export class InvoiceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getInvoices(filters?: InvoiceFilterParams): Observable<InvoiceSummary[]> {
    let params = new HttpParams();
    if (filters?.clientName) {
      params = params.set('clientName', filters.clientName);
    }
    if (filters?.fromDate) {
      params = params.set('fromDate', filters.fromDate);
    }
    if (filters?.toDate) {
      params = params.set('toDate', filters.toDate);
    }
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    if (filters?.clientId) {
      params = params.set('clientId', filters.clientId);
    }
    return this.http.get<InvoiceSummary[]>(`${this.baseUrl}/invoices`, { params });
  }

  getInvoice(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.baseUrl}/invoices/${id}`);
  }

  generateInvoice(bookingId: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices`, { bookingId });
  }

  sendInvoice(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/${id}/send`, null);
  }

  markAsPaid(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/${id}/pay`, null);
  }

  voidInvoice(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/${id}/void`, null);
  }

  addLineItem(invoiceId: string, lineItem: AddLineItemRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/${invoiceId}/line-items`, lineItem);
  }

  removeLineItem(invoiceId: string, lineItemId: string): Observable<Invoice> {
    return this.http.delete<Invoice>(
      `${this.baseUrl}/invoices/${invoiceId}/line-items/${lineItemId}`,
    );
  }

  regenerateInvoice(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/${id}/regenerate`, null);
  }

  getCompanyInfo(): Observable<CompanyInfo> {
    return this.http.get<CompanyInfo>(`${this.baseUrl}/settings/company`);
  }
}
