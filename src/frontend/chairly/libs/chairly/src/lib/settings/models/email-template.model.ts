export interface EmailTemplateResponse {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
  dateLabel: string | null;
  servicesLabel: string | null;
  isCustomized: boolean;
  availablePlaceholders: string[];
}

export interface UpdateEmailTemplateRequest {
  subject: string;
  mainMessage: string;
  closingMessage: string;
  dateLabel: string | null;
  servicesLabel: string | null;
}

export interface PreviewEmailTemplateRequest {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
  dateLabel: string | null;
  servicesLabel: string | null;
}

export interface PreviewEmailTemplateResponse {
  subject: string;
  htmlBody: string;
}
