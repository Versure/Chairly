import { formatDurationToTimeSpan, parseDuration } from './duration.utils';

describe('parseDuration', () => {
  it('should return 0 for "00:00:00"', () => {
    expect(parseDuration('00:00:00')).toBe(0);
  });

  it('should return 45 for "00:45:00"', () => {
    expect(parseDuration('00:45:00')).toBe(45);
  });

  it('should return 90 for "01:30:00"', () => {
    expect(parseDuration('01:30:00')).toBe(90);
  });

  it('should return 120 for "02:00:00"', () => {
    expect(parseDuration('02:00:00')).toBe(120);
  });

  it('should return 30 for "00:30:00"', () => {
    expect(parseDuration('00:30:00')).toBe(30);
  });

  it('should return 60 for "01:00:00"', () => {
    expect(parseDuration('01:00:00')).toBe(60);
  });
});

describe('formatDurationToTimeSpan', () => {
  it('should return "00:00:00" for 0 minutes', () => {
    expect(formatDurationToTimeSpan(0)).toBe('00:00:00');
  });

  it('should return "01:00:00" for 60 minutes', () => {
    expect(formatDurationToTimeSpan(60)).toBe('01:00:00');
  });

  it('should return "01:30:00" for 90 minutes', () => {
    expect(formatDurationToTimeSpan(90)).toBe('01:30:00');
  });

  it('should return "00:45:00" for 45 minutes', () => {
    expect(formatDurationToTimeSpan(45)).toBe('00:45:00');
  });

  it('should return "02:00:00" for 120 minutes', () => {
    expect(formatDurationToTimeSpan(120)).toBe('02:00:00');
  });
});
