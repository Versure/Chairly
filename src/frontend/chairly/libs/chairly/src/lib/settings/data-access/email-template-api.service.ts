import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  EmailTemplateResponse,
  PreviewEmailTemplateRequest,
  PreviewEmailTemplateResponse,
  UpdateEmailTemplateRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class EmailTemplateApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getEmailTemplates(): Observable<EmailTemplateResponse[]> {
    return this.http.get<EmailTemplateResponse[]>(`${this.baseUrl}/notifications/email-templates`);
  }

  updateEmailTemplate(
    templateType: string,
    request: UpdateEmailTemplateRequest,
  ): Observable<EmailTemplateResponse> {
    return this.http.put<EmailTemplateResponse>(
      `${this.baseUrl}/notifications/email-templates/${templateType}`,
      request,
    );
  }

  resetEmailTemplate(templateType: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/notifications/email-templates/${templateType}`);
  }

  previewEmailTemplate(
    request: PreviewEmailTemplateRequest,
  ): Observable<PreviewEmailTemplateResponse> {
    return this.http.post<PreviewEmailTemplateResponse>(
      `${this.baseUrl}/notifications/email-templates/preview`,
      request,
    );
  }
}
