export interface EmailTemplateResponse {
  templateType: string;
  subject: string;
  body: string;
  isCustomized: boolean;
  availablePlaceholders: string[];
}

export interface UpdateEmailTemplateRequest {
  subject: string;
  body: string;
}

export interface PreviewEmailTemplateRequest {
  templateType: string;
  subject: string;
  body: string;
}

export interface PreviewEmailTemplateResponse {
  subject: string;
  htmlBody: string;
}
