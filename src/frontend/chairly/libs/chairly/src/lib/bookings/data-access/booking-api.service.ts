import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { Booking, BookingFilter, CreateBookingRequest, UpdateBookingRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getBookings(filter?: BookingFilter): Observable<Booking[]> {
    let params = new HttpParams();
    if (filter?.date) {
      params = params.set('date', filter.date);
    }
    if (filter?.staffMemberId) {
      params = params.set('staffMemberId', filter.staffMemberId);
    }
    return this.http.get<Booking[]>(`${this.baseUrl}/bookings`, { params });
  }

  getBooking(id: string): Observable<Booking> {
    return this.http.get<Booking>(`${this.baseUrl}/bookings/${id}`);
  }

  createBooking(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(`${this.baseUrl}/bookings`, request);
  }

  updateBooking(id: string, request: UpdateBookingRequest): Observable<Booking> {
    return this.http.put<Booking>(`${this.baseUrl}/bookings/${id}`, request);
  }

  cancelBooking(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bookings/${id}/cancel`, null);
  }

  confirmBooking(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bookings/${id}/confirm`, null);
  }

  startBooking(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bookings/${id}/start`, null);
  }

  completeBooking(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bookings/${id}/complete`, null);
  }

  markNoShow(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/bookings/${id}/no-show`, null);
  }
}
