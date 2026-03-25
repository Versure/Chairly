export interface DashboardBooking {
  id: string;
  clientId: string;
  clientName: string;
  staffMemberId: string;
  staffMemberName: string;
  startTime: string;
  endTime: string;
  status: string;
  serviceNames: string[];
}

export interface DashboardResponse {
  todaysBookingsCount: number;
  todaysBookings: DashboardBooking[];
  upcomingBookings: DashboardBooking[];
  newClientsThisWeek: number;
  revenueThisWeek: number | null;
  revenueThisMonth: number | null;
}
