import { TemplateTypeLabelPipe } from './template-type-label.pipe';

describe('TemplateTypeLabelPipe', () => {
  const pipe = new TemplateTypeLabelPipe();

  it('transforms BookingConfirmation to Boekingsbevestiging', () => {
    expect(pipe.transform('BookingConfirmation')).toBe('Boekingsbevestiging');
  });

  it('transforms BookingReminder to Boekingsherinnering', () => {
    expect(pipe.transform('BookingReminder')).toBe('Boekingsherinnering');
  });

  it('transforms BookingCancellation to Boekingsannulering', () => {
    expect(pipe.transform('BookingCancellation')).toBe('Boekingsannulering');
  });

  it('transforms BookingReceived to Boeking ontvangen', () => {
    expect(pipe.transform('BookingReceived')).toBe('Boeking ontvangen');
  });

  it('transforms InvoiceSent to Factuur verzonden', () => {
    expect(pipe.transform('InvoiceSent')).toBe('Factuur verzonden');
  });

  it('returns the raw input string for unknown type values', () => {
    expect(pipe.transform('SomeUnknownType')).toBe('SomeUnknownType');
  });
});
