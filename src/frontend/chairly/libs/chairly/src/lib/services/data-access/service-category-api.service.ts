import { HttpClient } from '@angular/common/http';
import { inject,Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateServiceCategoryRequest,
  ServiceCategoryResponse,
  UpdateServiceCategoryRequest,
} from '../util';

@Injectable({ providedIn: 'root' })
export class ServiceCategoryApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<ServiceCategoryResponse[]> {
    return this.http.get<ServiceCategoryResponse[]>(
      `${this.baseUrl}/service-categories`
    );
  }

  create(
    request: CreateServiceCategoryRequest
  ): Observable<ServiceCategoryResponse> {
    return this.http.post<ServiceCategoryResponse>(
      `${this.baseUrl}/service-categories`,
      request
    );
  }

  update(
    id: string,
    request: UpdateServiceCategoryRequest
  ): Observable<ServiceCategoryResponse> {
    return this.http.put<ServiceCategoryResponse>(
      `${this.baseUrl}/service-categories/${id}`,
      request
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/service-categories/${id}`);
  }
}
