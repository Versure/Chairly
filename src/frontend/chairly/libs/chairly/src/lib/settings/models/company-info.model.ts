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

export interface UpdateCompanyInfoRequest {
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
