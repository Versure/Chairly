import { SubscriptionStatusBadgePipe } from './subscription-status-badge.pipe';

describe('SubscriptionStatusBadgePipe', () => {
  const pipe = new SubscriptionStatusBadgePipe();

  it('should return "In afwachting" for pending status', () => {
    const result = pipe.transform('pending');
    expect(result.label).toBe('In afwachting');
    expect(result.cssClass).toContain('bg-yellow-100');
  });

  it('should return "Proefperiode" for trial status', () => {
    const result = pipe.transform('trial');
    expect(result.label).toBe('Proefperiode');
    expect(result.cssClass).toContain('bg-blue-100');
  });

  it('should return "Actief" for provisioned status', () => {
    const result = pipe.transform('provisioned');
    expect(result.label).toBe('Actief');
    expect(result.cssClass).toContain('bg-green-100');
  });

  it('should return "Geannuleerd" for cancelled status', () => {
    const result = pipe.transform('cancelled');
    expect(result.label).toBe('Geannuleerd');
    expect(result.cssClass).toContain('bg-red-100');
  });

  it('should return the raw status for unknown values', () => {
    const result = pipe.transform('unknown');
    expect(result.label).toBe('unknown');
    expect(result.cssClass).toContain('bg-gray-100');
  });
});
