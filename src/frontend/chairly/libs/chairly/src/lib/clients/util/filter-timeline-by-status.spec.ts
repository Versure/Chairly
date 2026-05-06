import { filterByStatus } from './filter-timeline-by-status';

interface TestEntry {
  booking: { status: string };
}

function makeEntry(status: string): TestEntry {
  return {
    booking: { status },
  };
}

describe('filterByStatus', () => {
  const entries: TestEntry[] = [
    makeEntry('Scheduled'),
    makeEntry('Confirmed'),
    makeEntry('InProgress'),
    makeEntry('Completed'),
    makeEntry('Cancelled'),
    makeEntry('NoShow'),
  ];

  it('should return all entries when status is "All"', () => {
    const result = filterByStatus(entries, 'All');
    expect(result).toHaveLength(6);
  });

  it('should return only Completed entries when status is "Completed"', () => {
    const result = filterByStatus(entries, 'Completed');
    expect(result).toHaveLength(1);
    expect(result[0].booking.status).toBe('Completed');
  });

  it('should return only Cancelled entries when status is "Cancelled"', () => {
    const result = filterByStatus(entries, 'Cancelled');
    expect(result).toHaveLength(1);
    expect(result[0].booking.status).toBe('Cancelled');
  });

  it('should return only NoShow entries when status is "NoShow"', () => {
    const result = filterByStatus(entries, 'NoShow');
    expect(result).toHaveLength(1);
    expect(result[0].booking.status).toBe('NoShow');
  });

  it('should return Scheduled, Confirmed, and InProgress when status is "Scheduled" (Gepland)', () => {
    const result = filterByStatus(entries, 'Scheduled');
    expect(result).toHaveLength(3);
    const statuses = result.map((e) => e.booking.status);
    expect(statuses).toContain('Scheduled');
    expect(statuses).toContain('Confirmed');
    expect(statuses).toContain('InProgress');
  });

  it('should return empty array when no entries match the status', () => {
    const completedOnly = [makeEntry('Completed')];
    const result = filterByStatus(completedOnly, 'Cancelled');
    expect(result).toHaveLength(0);
  });
});
