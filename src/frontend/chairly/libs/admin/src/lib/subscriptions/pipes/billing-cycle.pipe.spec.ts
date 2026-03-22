import { BillingCyclePipe } from './billing-cycle.pipe';

describe('BillingCyclePipe', () => {
  const pipe = new BillingCyclePipe();

  it('should return "Maandelijks" for Monthly', () => {
    expect(pipe.transform('Monthly')).toBe('Maandelijks');
  });

  it('should return "Jaarlijks" for Annual', () => {
    expect(pipe.transform('Annual')).toBe('Jaarlijks');
  });

  it('should return "N.v.t." for null', () => {
    expect(pipe.transform(null)).toBe('N.v.t.');
  });

  it('should return "N.v.t." for unknown values', () => {
    expect(pipe.transform('Weekly')).toBe('N.v.t.');
  });
});
