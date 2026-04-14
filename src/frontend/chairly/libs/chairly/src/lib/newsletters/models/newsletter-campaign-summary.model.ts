import { NewsletterStatus } from './newsletter-status.model';

export interface NewsletterCampaignSummary {
  id: string;
  subject: string;
  status: NewsletterStatus;
  recipientCount: number;
  sentCount: number;
  failedCount: number;
  scheduledAtUtc?: string | null;
  sentAtUtc?: string | null;
  createdAtUtc: string;
}
