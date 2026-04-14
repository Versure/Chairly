import { NewsletterStatusLabelPipe } from './newsletter-status-label.pipe';

describe('NewsletterStatusLabelPipe', () => {
  const pipe = new NewsletterStatusLabelPipe();

  it('maps Draft to Concept', () => {
    expect(pipe.transform('Draft')).toBe('Concept');
  });

  it('maps Scheduled to Ingepland', () => {
    expect(pipe.transform('Scheduled')).toBe('Ingepland');
  });

  it('maps Sending to Wordt verzonden', () => {
    expect(pipe.transform('Sending')).toBe('Wordt verzonden');
  });

  it('maps Sent to Verzonden', () => {
    expect(pipe.transform('Sent')).toBe('Verzonden');
  });

  it('maps Cancelled to Geannuleerd', () => {
    expect(pipe.transform('Cancelled')).toBe('Geannuleerd');
  });

  it('returns the raw value for unknown statuses', () => {
    expect(pipe.transform('Unknown')).toBe('Unknown');
  });
});
