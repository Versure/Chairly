import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  AdminSubscriptionDetail,
  AdminSubscriptionsListResponse,
  CancelSubscriptionPayload,
  SubscriptionListFilters,
  UpdateSubscriptionPlanPayload,
} from '../models';

@Injectable({ providedIn: 'root' })
export class AdminSubscriptionApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getSubscriptions(filters: SubscriptionListFilters): Observable<AdminSubscriptionsListResponse> {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);

    if (filters.search) {
      params = params.set('search', filters.search);
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.plan) {
      params = params.set('plan', filters.plan);
    }

    return this.http.get<AdminSubscriptionsListResponse>(`${this.baseUrl}/admin/subscriptions`, {
      params,
    });
  }

  getSubscription(id: string): Observable<AdminSubscriptionDetail> {
    return this.http.get<AdminSubscriptionDetail>(`${this.baseUrl}/admin/subscriptions/${id}`);
  }

  provisionSubscription(id: string): Observable<AdminSubscriptionDetail> {
    return this.http.post<AdminSubscriptionDetail>(
      `${this.baseUrl}/admin/subscriptions/${id}/provision`,
      {},
    );
  }

  cancelSubscription(
    id: string,
    payload: CancelSubscriptionPayload,
  ): Observable<AdminSubscriptionDetail> {
    return this.http.post<AdminSubscriptionDetail>(
      `${this.baseUrl}/admin/subscriptions/${id}/cancel`,
      payload,
    );
  }

  updateSubscriptionPlan(
    id: string,
    payload: UpdateSubscriptionPlanPayload,
  ): Observable<AdminSubscriptionDetail> {
    return this.http.put<AdminSubscriptionDetail>(
      `${this.baseUrl}/admin/subscriptions/${id}/plan`,
      payload,
    );
  }
}
