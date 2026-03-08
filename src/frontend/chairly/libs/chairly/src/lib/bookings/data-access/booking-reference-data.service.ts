import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { ClientOption, ServiceOption, StaffMemberOption } from '../models';

@Injectable({ providedIn: 'root' })
export class BookingReferenceDataService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getClients(): Observable<ClientOption[]> {
    return this.http.get<ClientOption[]>(`${this.baseUrl}/clients`);
  }

  getStaffMembers(): Observable<StaffMemberOption[]> {
    return this.http.get<StaffMemberOption[]>(`${this.baseUrl}/staff`);
  }

  getServices(): Observable<ServiceOption[]> {
    return this.http.get<ServiceOption[]>(`${this.baseUrl}/services`);
  }
}
