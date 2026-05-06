import { formatMonthLabel } from './format-month-label';

interface EntryWithBookingStartTime {
  booking: { startTime: string };
}

export interface MonthGroup<T = unknown> {
  monthKey: string;
  label: string;
  entries: T[];
}

export function groupByMonth<T extends EntryWithBookingStartTime>(entries: T[]): MonthGroup<T>[] {
  const map = new Map<string, MonthGroup<T>>();

  for (const entry of entries) {
    const date = new Date(entry.booking.startTime);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const monthKey = `${year}-${month}`;

    let group = map.get(monthKey);
    if (!group) {
      group = {
        monthKey,
        label: formatMonthLabel(date),
        entries: [],
      };
      map.set(monthKey, group);
    }
    group.entries.push(entry);
  }

  return Array.from(map.values()).sort((a, b) => b.monthKey.localeCompare(a.monthKey));
}
