import { PaymentMethod } from '@org/shared-lib';

export type PeriodType = 'week' | 'month' | 'year';

export interface RevenueReportRow {
  date: string;
  invoiceNumber: string;
  totalAmount: number;
  vatAmount: number;
  paymentMethod: PaymentMethod;
}

export interface RevenueReportDailyTotal {
  date: string;
  totalAmount: number;
  vatAmount: number;
  invoiceCount: number;
}

export interface RevenueReportGrandTotal {
  totalAmount: number;
  vatAmount: number;
  invoiceCount: number;
}

export interface RevenueReport {
  periodType: PeriodType;
  periodStart: string;
  periodEnd: string;
  salonName: string;
  rows: RevenueReportRow[];
  dailyTotals: RevenueReportDailyTotal[];
  grandTotal: RevenueReportGrandTotal;
}
