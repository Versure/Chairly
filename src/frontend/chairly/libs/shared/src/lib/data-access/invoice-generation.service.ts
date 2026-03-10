import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '../util';

/**
 * Minimal invoice generation service exposed via shared library
 * so the bookings domain can generate invoices without a direct
 * cross-domain import to the billing domain.
 */

export interface GenerateInvoiceResponse {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  totalAmount: number;
  status: string;
}

/** Lightweight invoice summary for cross-domain use (e.g. client detail page). */
export interface ClientInvoiceSummary {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  totalAmount: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class InvoiceGenerationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  generateInvoice(bookingId: string): Observable<GenerateInvoiceResponse> {
    return this.http.post<GenerateInvoiceResponse>(`${this.baseUrl}/invoices`, { bookingId });
  }

  getClientInvoices(clientId: string): Observable<ClientInvoiceSummary[]> {
    return this.http.get<ClientInvoiceSummary[]>(`${this.baseUrl}/invoices`, {
      params: { clientId },
    });
  }
}
