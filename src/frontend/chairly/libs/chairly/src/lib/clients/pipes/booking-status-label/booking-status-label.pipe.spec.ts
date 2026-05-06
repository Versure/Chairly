import { BookingStatus } from '../../models';
import { BookingStatusLabelPipe } from './booking-status-label.pipe';

describe('BookingStatusLabelPipe', () => {
  const pipe = new BookingStatusLabelPipe();

  it('should transform "Scheduled" to "Gepland"', () => {
    expect(pipe.transform('Scheduled')).toBe('Gepland');
  });

  it('should transform "Confirmed" to "Bevestigd"', () => {
    expect(pipe.transform('Confirmed')).toBe('Bevestigd');
  });

  it('should transform "InProgress" to "Bezig"', () => {
    expect(pipe.transform('InProgress')).toBe('Bezig');
  });

  it('should transform "Completed" to "Voltooid"', () => {
    expect(pipe.transform('Completed')).toBe('Voltooid');
  });

  it('should transform "Cancelled" to "Geannuleerd"', () => {
    expect(pipe.transform('Cancelled')).toBe('Geannuleerd');
  });

  it('should transform "NoShow" to "No-show"', () => {
    expect(pipe.transform('NoShow')).toBe('No-show');
  });

  it('should return the original value for an unknown status', () => {
    expect(pipe.transform('Unknown' as BookingStatus)).toBe('Unknown');
  });
});
