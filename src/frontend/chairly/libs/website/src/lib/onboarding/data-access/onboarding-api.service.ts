import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { CreateSubscriptionPayload, SubscriptionPlanInfo, SubscriptionResponse } from '../models';

@Injectable({ providedIn: 'root' })
export class OnboardingApiService {
  private readonly http = inject(HttpClient);

  getSubscriptionPlans(): Observable<SubscriptionPlanInfo[]> {
    return this.http.get<SubscriptionPlanInfo[]>('/api/onboarding/plans');
  }

  createSubscription(payload: CreateSubscriptionPayload): Observable<SubscriptionResponse> {
    return this.http.post<SubscriptionResponse>('/api/onboarding/subscriptions', payload);
  }
}
