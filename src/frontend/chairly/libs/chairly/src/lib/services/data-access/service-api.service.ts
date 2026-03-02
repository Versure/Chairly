import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateServiceRequest,
  ServiceResponse,
  UpdateServiceRequest,
} from '../util';

@Injectable({ providedIn: 'root' })
export class ServiceApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<ServiceResponse[]> {
    return this.http.get<ServiceResponse[]>(`${this.baseUrl}/services`);
  }

  getById(id: string): Observable<ServiceResponse> {
    return this.http.get<ServiceResponse>(`${this.baseUrl}/services/${id}`);
  }

  create(request: CreateServiceRequest): Observable<ServiceResponse> {
    return this.http.post<ServiceResponse>(`${this.baseUrl}/services`, request);
  }

  update(id: string, request: UpdateServiceRequest): Observable<ServiceResponse> {
    return this.http.put<ServiceResponse>(
      `${this.baseUrl}/services/${id}`,
      request
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/services/${id}`);
  }

  toggleActive(id: string): Observable<ServiceResponse> {
    return this.http.patch<ServiceResponse>(
      `${this.baseUrl}/services/${id}/toggle-active`,
      null
    );
  }
}
