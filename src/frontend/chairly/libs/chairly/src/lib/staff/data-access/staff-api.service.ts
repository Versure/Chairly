import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  CreateStaffMemberRequest,
  StaffMemberResponse,
  UpdateStaffMemberRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class StaffApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<StaffMemberResponse[]> {
    return this.http.get<StaffMemberResponse[]>(`${this.baseUrl}/staff`);
  }

  create(request: CreateStaffMemberRequest): Observable<StaffMemberResponse> {
    return this.http.post<StaffMemberResponse>(`${this.baseUrl}/staff`, request);
  }

  update(id: string, request: UpdateStaffMemberRequest): Observable<StaffMemberResponse> {
    return this.http.put<StaffMemberResponse>(`${this.baseUrl}/staff/${id}`, request);
  }

  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/staff/${id}/deactivate`, null);
  }

  reactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/staff/${id}/reactivate`, null);
  }
}
