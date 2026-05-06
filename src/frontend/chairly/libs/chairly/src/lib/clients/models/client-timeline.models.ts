import { ClientResponse } from './client.models';
import { ClientRecipeSummary } from './recipe.models';

export type BookingStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Void';

export interface BookingTimelineService {
  serviceId: string;
  serviceName: string;
  durationMinutes: number;
  price: number;
  sortOrder: number;
}

export interface BookingTimelineCard {
  id: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: BookingStatus;
  staffMemberId: string;
  staffMemberName: string;
  totalPrice: number;
  notes: string | null;
  services: BookingTimelineService[];
  confirmedAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  noShowAtUtc: string | null;
}

export interface ClientTimelineInvoice {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  totalAmount: number;
  status: InvoiceStatus;
  sentAtUtc: string | null;
  paidAtUtc: string | null;
  voidedAtUtc: string | null;
}

export interface StaffMemberSummary {
  id: string;
  fullName: string;
  visitCount: number;
}

export interface ServiceSummary {
  id: string;
  name: string;
  bookingCount: number;
}

export interface ClientTimelineStats {
  totalVisits: number;
  lastVisitAtUtc: string | null;
  totalSpentAmount: number;
  mostVisitedStaffMember: StaffMemberSummary | null;
  mostBookedService: ServiceSummary | null;
  noShowCount: number;
}

export interface ClientTimelineEntry {
  booking: BookingTimelineCard;
  recipe: ClientRecipeSummary | null;
  invoice: ClientTimelineInvoice | null;
}

export interface ClientTimeline {
  profile: ClientResponse;
  stats: ClientTimelineStats;
  timeline: ClientTimelineEntry[];
}
