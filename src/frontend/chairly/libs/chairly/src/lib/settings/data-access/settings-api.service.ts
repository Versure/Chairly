import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { CompanyInfo, UpdateCompanyInfoRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getCompanyInfo(): Observable<CompanyInfo> {
    return this.http.get<CompanyInfo>(`${this.baseUrl}/settings/company`);
  }

  updateCompanyInfo(request: UpdateCompanyInfoRequest): Observable<CompanyInfo> {
    return this.http.put<CompanyInfo>(`${this.baseUrl}/settings/company`, request);
  }
}
