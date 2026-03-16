import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable, tap } from 'rxjs';

import { API_BASE_URL } from '../util';
import { AppConfig } from './models/app-config.model';

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private config: AppConfig | null = null;

  load(): Observable<AppConfig> {
    return this.http.get<AppConfig>(`${this.baseUrl}/config`).pipe(tap((c) => (this.config = c)));
  }

  get keycloakUrl(): string {
    if (!this.config) {
      throw new Error('AppConfig not loaded yet. Call load() first.');
    }
    return this.config.keycloakUrl;
  }

  get keycloakRealm(): string {
    if (!this.config) {
      throw new Error('AppConfig not loaded yet. Call load() first.');
    }
    return this.config.keycloakRealm;
  }

  get keycloakClientId(): string {
    if (!this.config) {
      throw new Error('AppConfig not loaded yet. Call load() first.');
    }
    return this.config.keycloakClientId;
  }
}
