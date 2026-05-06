interface EntryWithBookingStatus {
  booking: { status: string };
}

const GEPLAND_STATUSES = new Set(['Scheduled', 'Confirmed', 'InProgress']);

export function filterByStatus<T extends EntryWithBookingStatus>(
  entries: T[],
  status: string,
): T[] {
  if (status === 'All') {
    return entries;
  }

  if (status === 'Scheduled') {
    return entries.filter((e) => GEPLAND_STATUSES.has(e.booking.status));
  }

  return entries.filter((e) => e.booking.status === status);
}
