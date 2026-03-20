// Inline type definition to avoid importing from models/ (util/ has noDependencies in Sheriff)
type NotificationType =
  | 'BookingConfirmation'
  | 'BookingReminder'
  | 'BookingCancellation'
  | 'BookingReceived'
  | 'InvoiceSent';

const typeLabels: Record<NotificationType, string> = {
  BookingConfirmation: 'Bevestiging',
  BookingReminder: 'Herinnering',
  BookingCancellation: 'Annulering',
  BookingReceived: 'Ontvangen',
  InvoiceSent: 'Factuur verzonden',
};

export function notificationTypeLabel(type: NotificationType): string {
  return typeLabels[type];
}
