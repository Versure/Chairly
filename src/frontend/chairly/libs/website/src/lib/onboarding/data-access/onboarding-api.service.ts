import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import {
  DemoRequestResponse,
  SignUpRequestResponse,
  SubmitDemoRequestPayload,
  SubmitSignUpRequestPayload,
} from '../models';

@Injectable({ providedIn: 'root' })
export class OnboardingApiService {
  private readonly http = inject(HttpClient);

  submitDemoRequest(payload: SubmitDemoRequestPayload): Observable<DemoRequestResponse> {
    return this.http.post<DemoRequestResponse>('/api/onboarding/demo-requests', payload);
  }

  submitSignUpRequest(payload: SubmitSignUpRequestPayload): Observable<SignUpRequestResponse> {
    return this.http.post<SignUpRequestResponse>('/api/onboarding/sign-up-requests', payload);
  }
}
