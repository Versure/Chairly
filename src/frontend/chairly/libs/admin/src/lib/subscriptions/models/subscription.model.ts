export interface AdminSubscriptionListItem {
  id: string;
  salonName: string;
  ownerName: string;
  email: string;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
  status: string;
  createdAtUtc: string;
  provisionedAtUtc: string | null;
  cancelledAtUtc: string | null;
}

export interface AdminSubscriptionsListResponse {
  items: AdminSubscriptionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminSubscriptionDetail {
  id: string;
  salonName: string;
  ownerFirstName: string;
  ownerLastName: string;
  email: string;
  phoneNumber: string | null;
  plan: string;
  billingCycle: string | null;
  isTrial: boolean;
  status: string;
  trialEndsAtUtc: string | null;
  createdAtUtc: string;
  createdBy: string | null;
  provisionedAtUtc: string | null;
  provisionedBy: string | null;
  cancelledAtUtc: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
}

export interface CancelSubscriptionPayload {
  cancellationReason: string;
}

export interface UpdateSubscriptionPlanPayload {
  plan: string;
  billingCycle: string | null;
}

export interface SubscriptionListFilters {
  search: string;
  status: string;
  plan: string;
  page: number;
  pageSize: number;
}
