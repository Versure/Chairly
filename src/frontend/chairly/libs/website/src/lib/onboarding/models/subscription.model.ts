export interface SubscriptionPlanInfo {
  slug: string;
  name: string;
  maxStaff: number;
  monthlyPrice: number;
  annualPricePerMonth: number;
}

export interface CreateSubscriptionPayload {
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
}

export interface SubscriptionResponse {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
  trialEndsAtUtc: string | null;
  createdAtUtc: string;
}
