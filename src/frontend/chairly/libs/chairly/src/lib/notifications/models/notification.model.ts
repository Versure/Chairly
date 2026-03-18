export type NotificationType =
  | 'BookingConfirmation'
  | 'BookingReminder'
  | 'BookingCancellation'
  | 'BookingReceived'
  | 'InvoiceSent';
export type NotificationChannel = 'Email' | 'Sms';
export type NotificationStatus = 'Wachtend' | 'Verzonden' | 'Mislukt';

export interface NotificationSummary {
  id: string;
  type: NotificationType;
  recipientName: string;
  channel: NotificationChannel;
  status: NotificationStatus;
  scheduledAtUtc: string;
  sentAtUtc?: string;
  failedAtUtc?: string;
  failureReason?: string;
  retryCount: number;
  referenceId: string;
}
