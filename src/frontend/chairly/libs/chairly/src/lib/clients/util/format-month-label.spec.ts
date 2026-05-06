import { formatMonthLabel } from './format-month-label';

describe('formatMonthLabel', () => {
  it('should return a capitalized Dutch month and year for May 2026', () => {
    const result = formatMonthLabel(new Date(2026, 4, 14));
    expect(result).toBe('Mei 2026');
  });

  it('should return a capitalized Dutch month and year for January 2025', () => {
    const result = formatMonthLabel(new Date(2025, 0, 1));
    expect(result).toBe('Januari 2025');
  });

  it('should return a capitalized Dutch month and year for December 2024', () => {
    const result = formatMonthLabel(new Date(2024, 11, 31));
    expect(result).toBe('December 2024');
  });

  it('should handle March correctly', () => {
    const result = formatMonthLabel(new Date(2026, 2, 15));
    expect(result).toBe('Maart 2026');
  });
});
