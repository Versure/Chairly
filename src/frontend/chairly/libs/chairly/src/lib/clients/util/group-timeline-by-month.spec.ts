import { groupByMonth, MonthGroup } from './group-timeline-by-month';

interface TestEntry {
  booking: { startTime: string };
}

function makeEntry(startTime: string): TestEntry {
  return {
    booking: { startTime },
  };
}

describe('groupByMonth', () => {
  it('should group entries by month and sort months descending', () => {
    const entries: TestEntry[] = [
      makeEntry('2026-05-14T10:00:00Z'),
      makeEntry('2026-05-02T09:00:00Z'),
      makeEntry('2026-03-15T14:00:00Z'),
      makeEntry('2026-01-20T11:00:00Z'),
    ];

    const groups: MonthGroup<TestEntry>[] = groupByMonth(entries);

    expect(groups).toHaveLength(3);
    expect(groups[0].monthKey).toBe('2026-05');
    expect(groups[0].entries).toHaveLength(2);
    expect(groups[1].monthKey).toBe('2026-03');
    expect(groups[1].entries).toHaveLength(1);
    expect(groups[2].monthKey).toBe('2026-01');
    expect(groups[2].entries).toHaveLength(1);
  });

  it('should return empty array for empty input', () => {
    const groups = groupByMonth([]);
    expect(groups).toHaveLength(0);
  });

  it('should produce a capitalized Dutch label for each group', () => {
    const entries: TestEntry[] = [makeEntry('2026-01-15T10:00:00Z')];

    const groups = groupByMonth(entries);

    expect(groups[0].label).toBe('Januari 2026');
  });

  it('should handle entries spanning multiple years', () => {
    const entries: TestEntry[] = [
      makeEntry('2026-12-01T10:00:00Z'),
      makeEntry('2025-11-15T10:00:00Z'),
    ];

    const groups = groupByMonth(entries);

    expect(groups).toHaveLength(2);
    expect(groups[0].monthKey).toBe('2026-12');
    expect(groups[1].monthKey).toBe('2025-11');
  });
});
