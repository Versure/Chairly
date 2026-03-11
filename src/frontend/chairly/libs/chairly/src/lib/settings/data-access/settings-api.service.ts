import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { VatSettings } from '../models';

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getVatSettings(): Observable<VatSettings> {
    return this.http.get<VatSettings>(`${this.baseUrl}/settings/vat`);
  }

  updateVatSettings(defaultVatRate: number): Observable<VatSettings> {
    return this.http.put<VatSettings>(`${this.baseUrl}/settings/vat`, {
      defaultVatRate,
    });
  }
}
