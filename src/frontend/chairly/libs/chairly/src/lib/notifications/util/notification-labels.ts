// Inline type definition to avoid importing from models/ (util/ has noDependencies in Sheriff)
type NotificationType = 'BookingConfirmation' | 'BookingReminder' | 'BookingCancellation';

const typeLabels: Record<NotificationType, string> = {
  BookingConfirmation: 'Bevestiging',
  BookingReminder: 'Herinnering',
  BookingCancellation: 'Annulering',
};

export function notificationTypeLabel(type: NotificationType): string {
  return typeLabels[type];
}
