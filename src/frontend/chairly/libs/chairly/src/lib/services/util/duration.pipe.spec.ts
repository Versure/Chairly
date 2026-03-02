import { DurationPipe } from './duration.pipe';

describe('DurationPipe', () => {
  let pipe: DurationPipe;

  beforeEach(() => {
    pipe = new DurationPipe();
  });

  it('should display minutes only when less than 1 hour', () => {
    expect(pipe.transform('00:30:00')).toBe('30 min');
  });

  it('should display hours only when exactly on the hour', () => {
    expect(pipe.transform('01:00:00')).toBe('1h');
  });

  it('should display hours and minutes when both are non-zero', () => {
    expect(pipe.transform('01:30:00')).toBe('1h 30min');
  });

  it('should display "0 min" for zero duration', () => {
    expect(pipe.transform('00:00:00')).toBe('0 min');
  });

  it('should display "45 min" for 45 minutes', () => {
    expect(pipe.transform('00:45:00')).toBe('45 min');
  });

  it('should display "2h" for 2 hours', () => {
    expect(pipe.transform('02:00:00')).toBe('2h');
  });

  it('should display "2h 15min" for 2 hours 15 minutes', () => {
    expect(pipe.transform('02:15:00')).toBe('2h 15min');
  });
});
