export interface CompanyInfo {
  companyName: string | null;
  companyEmail: string | null;
  companyAddress: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}

export interface UpdateCompanyInfoRequest {
  companyName: string | null;
  companyEmail: string | null;
  companyAddress: string | null;
  companyPhone: string | null;
  ibanNumber: string | null;
  vatNumber: string | null;
  paymentPeriodDays: number | null;
}
