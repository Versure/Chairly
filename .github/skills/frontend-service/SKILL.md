---
name: frontend-service
description: >
  API service patterns for Chairly frontend using HttpClient.
  Use when creating services that call backend endpoints.
---

# API Service Pattern

Location: `libs/chairly/src/lib/{domain}/data-access/{entity}-api.service.ts`

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import {
  {Entity}Response,
  Create{Entity}Request,
  Update{Entity}Request,
} from '../models';

@Injectable()
export class {Entity}ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<{Entity}Response[]> {
    return this.http.get<{Entity}Response[]>(`${this.baseUrl}/api/{context}`);
  }

  getById(id: string): Observable<{Entity}Response> {
    return this.http.get<{Entity}Response>(`${this.baseUrl}/api/{context}/${id}`);
  }

  create(request: Create{Entity}Request): Observable<{Entity}Response> {
    return this.http.post<{Entity}Response>(`${this.baseUrl}/api/{context}`, request);
  }

  update(id: string, request: Update{Entity}Request): Observable<{Entity}Response> {
    return this.http.put<{Entity}Response>(`${this.baseUrl}/api/{context}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/{context}/${id}`);
  }
}
```

## Rules

- One service per backend context
- Injectable at route level (not `providedIn: 'root'`)
- Use `API_BASE_URL` injection token from shared lib
- Explicit return types on all methods
- One method per HTTP verb/endpoint
