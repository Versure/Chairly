import { NewsletterStatus } from './newsletter-status.model';

export interface NewsletterCampaignDetail {
  id: string;
  subject: string;
  bodyHtml: string;
  recipientFilter: string;
  status: NewsletterStatus;
  scheduledAtUtc?: string | null;
  queuedAtUtc?: string | null;
  sentAtUtc?: string | null;
  cancelledAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
  pendingCount: number;
  unsubscribedCount: number;
  eligibleSubscribers: number;
}
