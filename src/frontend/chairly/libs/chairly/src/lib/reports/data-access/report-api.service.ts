import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { PeriodType, RevenueReport } from '../models';

@Injectable({ providedIn: 'root' })
export class ReportApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getRevenueReport(period: PeriodType, date: string): Observable<RevenueReport> {
    return this.http.get<RevenueReport>(`${this.baseUrl}/reports/revenue`, {
      params: { period, date },
    });
  }

  downloadRevenueReportPdf(period: PeriodType, date: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reports/revenue/pdf`, {
      params: { period, date },
      responseType: 'blob',
    });
  }
}
