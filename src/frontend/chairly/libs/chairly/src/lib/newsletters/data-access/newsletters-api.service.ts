import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateNewsletterCampaignRequest,
  NewsletterCampaignDetail,
  NewsletterCampaignSummary,
  PreviewNewsletterRequest,
  PreviewNewsletterResponse,
  ScheduleNewsletterCampaignRequest,
  UpdateNewsletterCampaignRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class NewslettersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get campaignsUrl(): string {
    return `${this.baseUrl}/newsletters/campaigns`;
  }

  listCampaigns(): Observable<NewsletterCampaignSummary[]> {
    return this.http.get<NewsletterCampaignSummary[]>(this.campaignsUrl);
  }

  getCampaign(id: string): Observable<NewsletterCampaignDetail> {
    return this.http.get<NewsletterCampaignDetail>(`${this.campaignsUrl}/${id}`);
  }

  createCampaign(request: CreateNewsletterCampaignRequest): Observable<NewsletterCampaignDetail> {
    return this.http.post<NewsletterCampaignDetail>(this.campaignsUrl, request);
  }

  updateCampaign(
    id: string,
    request: UpdateNewsletterCampaignRequest,
  ): Observable<NewsletterCampaignDetail> {
    return this.http.put<NewsletterCampaignDetail>(`${this.campaignsUrl}/${id}`, request);
  }

  deleteCampaign(id: string): Observable<void> {
    return this.http.delete<void>(`${this.campaignsUrl}/${id}`);
  }

  scheduleCampaign(
    id: string,
    request: ScheduleNewsletterCampaignRequest,
  ): Observable<NewsletterCampaignDetail> {
    return this.http.post<NewsletterCampaignDetail>(`${this.campaignsUrl}/${id}/schedule`, request);
  }

  cancelCampaign(id: string): Observable<NewsletterCampaignDetail> {
    return this.http.post<NewsletterCampaignDetail>(`${this.campaignsUrl}/${id}/cancel`, {});
  }

  sendCampaign(id: string): Observable<NewsletterCampaignDetail> {
    return this.http.post<NewsletterCampaignDetail>(`${this.campaignsUrl}/${id}/send`, {});
  }

  testSendCampaign(id: string): Observable<void> {
    return this.http.post<void>(`${this.campaignsUrl}/${id}/test-send`, {});
  }

  previewNewsletter(request: PreviewNewsletterRequest): Observable<PreviewNewsletterResponse> {
    return this.http.post<PreviewNewsletterResponse>(
      `${this.baseUrl}/newsletters/preview`,
      request,
    );
  }
}
