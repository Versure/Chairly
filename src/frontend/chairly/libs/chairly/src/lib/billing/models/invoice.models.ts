export type InvoiceStatus = 'Concept' | 'Verzonden' | 'Betaald' | 'Vervallen';

export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  vatPercentage: number;
  vatAmount: number;
  isManual: boolean;
  sortOrder: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  bookingId: string;
  clientId: string;
  clientFullName: string;
  subTotalAmount: number;
  totalVatAmount: number;
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
  subTotalAmount: number;
  totalVatAmount: number;
  totalAmount: number;
  status: InvoiceStatus;
  createdAtUtc: string;
  sentAtUtc?: string;
  paidAtUtc?: string;
  voidedAtUtc?: string;
}

export interface AddLineItemRequest {
  description: string;
  quantity: number;
  unitPrice: number;
  vatPercentage: number;
  isManual: boolean;
}
