import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { NotificationSummary } from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getNotifications(): Observable<NotificationSummary[]> {
    return this.http.get<NotificationSummary[]>(`${this.baseUrl}/notifications`);
  }
}
