export type InvoiceStatus = 'Concept' | 'Verzonden' | 'Betaald' | 'Vervallen';

export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  sortOrder: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  totalAmount: number;
  status: InvoiceStatus;
  lineItems: InvoiceLineItem[];
  createdAtUtc: string;
  sentAtUtc?: string;
  paidAtUtc?: string;
  voidedAtUtc?: string;
}

export interface InvoiceSummary {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  totalAmount: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  sentAtUtc?: string;
  paidAtUtc?: string;
  voidedAtUtc?: string;
}
