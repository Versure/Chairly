import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  ClientBookingSummary,
  ClientResponse,
  CreateClientRequest,
  UpdateClientRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class ClientApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<ClientResponse[]> {
    return this.http.get<ClientResponse[]>(`${this.baseUrl}/clients`);
  }

  create(request: CreateClientRequest): Observable<ClientResponse> {
    return this.http.post<ClientResponse>(`${this.baseUrl}/clients`, request);
  }

  update(id: string, request: UpdateClientRequest): Observable<ClientResponse> {
    return this.http.put<ClientResponse>(`${this.baseUrl}/clients/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clients/${id}`);
  }

  getClientBookings(clientId: string): Observable<ClientBookingSummary[]> {
    return this.http
      .get<(ClientBookingSummary & { clientId: string })[]>(`${this.baseUrl}/bookings`)
      .pipe(map((bookings) => bookings.filter((b) => b.clientId === clientId)));
  }
}
