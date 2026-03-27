export interface EmailTemplateResponse {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
  isCustomized: boolean;
  availablePlaceholders: string[];
}

export interface UpdateEmailTemplateRequest {
  subject: string;
  mainMessage: string;
  closingMessage: string;
}

export interface PreviewEmailTemplateRequest {
  templateType: string;
  subject: string;
  mainMessage: string;
  closingMessage: string;
}

export interface PreviewEmailTemplateResponse {
  subject: string;
  htmlBody: string;
}
