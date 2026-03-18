import { notificationTypeLabel } from './notification-labels';

describe('notificationTypeLabel', () => {
  it('returns Dutch label for InvoiceSent', () => {
    expect(notificationTypeLabel('InvoiceSent')).toBe('Factuur verzonden');
  });
});
