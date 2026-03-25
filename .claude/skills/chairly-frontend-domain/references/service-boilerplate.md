# API Service Boilerplate

Location: `libs/chairly/src/lib/{domain}/data-access/{entity}-api.service.ts`

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { API_BASE_URL } from '@org/shared-lib';

import { Create{Entity}Request, {Entity}Response, Update{Entity}Request } from '../models';

@Injectable({ providedIn: 'root' })
export class {Entity}ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<{Entity}Response[]> {
    return this.http.get<{Entity}Response[]>(`${this.baseUrl}/{entities}`);
  }

  getById(id: string): Observable<{Entity}Response> {
    return this.http.get<{Entity}Response>(`${this.baseUrl}/{entities}/${id}`);
  }

  create(request: Create{Entity}Request): Observable<{Entity}Response> {
    return this.http.post<{Entity}Response>(`${this.baseUrl}/{entities}`, request);
  }

  update(id: string, request: Update{Entity}Request): Observable<{Entity}Response> {
    return this.http.put<{Entity}Response>(`${this.baseUrl}/{entities}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/{entities}/${id}`);
  }
}
```

## Rules

- `@Injectable({ providedIn: 'root' })` — always root-provided
- Inject `API_BASE_URL` token from `@org/shared-lib` — never hardcode base URLs
- Return `Observable<T>` — never subscribe inside the service
- One method per HTTP operation; add domain-specific methods (e.g. `toggleActive`, `reorder`) as needed
- No error handling in the service — let the store catch errors
- URL segment matches the backend route group (e.g. `/services`, `/clients`, `/bookings`)
