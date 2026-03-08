export type BookingStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow';

export interface BookingServiceItem {
  serviceId: string;
  serviceName: string;
  duration: string; // "HH:MM:SS"
  price: number;
  sortOrder: number;
}

export interface Booking {
  id: string;
  clientId: string;
  staffMemberId: string;
  startTime: string; // ISO 8601
  endTime: string;
  notes: string | null;
  status: BookingStatus;
  services: BookingServiceItem[];
  createdAtUtc: string;
  updatedAtUtc: string | null;
  confirmedAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  noShowAtUtc: string | null;
}

export interface CreateBookingRequest {
  clientId: string;
  staffMemberId: string;
  startTime: string;
  serviceIds: string[];
  notes: string | null;
}

export type UpdateBookingRequest = CreateBookingRequest;

export interface BookingFilter {
  date?: string; // YYYY-MM-DD
  staffMemberId?: string;
}
