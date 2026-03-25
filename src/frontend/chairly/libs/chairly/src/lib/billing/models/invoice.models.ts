import { PaymentMethod } from '@org/shared-lib';

export type InvoiceStatus = 'Concept' | 'Verzonden' | 'Betaald' | 'Vervallen';

export interface MarkInvoicePaidRequest {
  paymentMethod: PaymentMethod;
}

export interface ClientSnapshot {
  fullName: string;
  email: string | null;
  phone: string | null;
  address: string | null;
}

export interface CompanyInfo {
  companyName: string | null;
  companyEmail: string | null;
  street: string | null;
  houseNumber: string | null;
  postalCode: string | null;
  city: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}

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
  clientSnapshot: ClientSnapshot;
  staffMemberName: string;
  subTotalAmount: number;
  totalVatAmount: number;
  totalAmount: number;
  paymentMethod: PaymentMethod;
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
  paymentMethod: PaymentMethod;
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

export interface InvoiceFilterParams {
  clientName?: string;
  fromDate?: string;
  toDate?: string;
  status?: InvoiceStatus | '';
  clientId?: string;
}
