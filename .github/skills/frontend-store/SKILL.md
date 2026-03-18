---
name: frontend-store
description: >
  NgRx SignalStore patterns for Chairly frontend.
  Use when creating or modifying state management stores.
---

# NgRx SignalStore Pattern

Location: `libs/chairly/src/lib/{domain}/data-access/{entity}.store.ts`

```typescript
import { computed, inject } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { tapResponse } from '@ngrx/operators';
import { pipe, switchMap, tap } from 'rxjs';

import { {Entity}Response } from '../models';
import { {Entity}ApiService } from './{entity}-api.service';

export interface {Entity}State {
  items: {Entity}Response[];
  loading: boolean;
  error: string | null;
}

const initialState: {Entity}State = {
  items: [],
  loading: false,
  error: null,
};

export const {Entity}Store = signalStore(
  withState(initialState),
  withComputed((store) => ({
    hasItems: computed(() => store.items().length > 0),
  })),
  withMethods((store, api = inject({Entity}ApiService)) => ({
    loadAll: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap(() =>
          api.getAll().pipe(
            tapResponse({
              next: (items) => patchState(store, { items, loading: false }),
              error: (err: Error) =>
                patchState(store, { error: err.message, loading: false }),
            }),
          ),
        ),
      ),
    ),
  })),
);
```

## Barrel export (`data-access/index.ts`)

```typescript
export type { {Entity}State } from './{entity}.store';
export { {Entity}Store } from './{entity}.store';
export { {Entity}ApiService } from './{entity}-api.service';
```

## Rules

- Store is provided at route level, not root
- Use `rxMethod` for async operations
- Use `tapResponse` for error handling
- Use `patchState` for state updates
- Explicit return types on all computed signals
