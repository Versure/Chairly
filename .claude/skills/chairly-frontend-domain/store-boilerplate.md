# NgRx SignalStore Boilerplate

Location: `libs/chairly/src/lib/{domain}/data-access/{entity}.store.ts`

```typescript
import { computed, inject } from '@angular/core';

import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { Create{Entity}Request, {Entity}Response, Update{Entity}Request } from '../models';
import { {Entity}ApiService } from './{entity}-api.service';

export interface {Entity}State {
  {entities}: {Entity}Response[];
  isLoading: boolean;
  error: string | null;
}

const initialState: {Entity}State = {
  {entities}: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

function replace{Entity}(
  {entities}: {Entity}Response[],
  id: string,
  updated: {Entity}Response
): {Entity}Response[] {
  return {entities}.map((item) => (item.id === id ? updated : item));
}

function remove{Entity}(
  {entities}: {Entity}Response[],
  id: string
): {Entity}Response[] {
  return {entities}.filter((item) => item.id !== id);
}

export const {Entity}Store = signalStore(
  withState<{Entity}State>(initialState),
  withComputed((store) => ({
    // Add domain-specific computed signals here, e.g.:
    // active{Entities}: computed(() => store.{entities}().filter((item) => item.isActive)),
  })),
  withMethods((store) => {
    const {entity}Api = inject({Entity}ApiService);

    return {
      load{Entities}(): void {
        patchState(store, { isLoading: true, error: null });
        {entity}Api
          .getAll()
          .pipe(take(1))
          .subscribe({
            next: ({entities}) =>
              patchState(store, { {entities}, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err), isLoading: false }),
          });
      },

      create{Entity}(request: Create{Entity}Request): void {
        {entity}Api
          .create(request)
          .pipe(take(1))
          .subscribe({
            next: (item) =>
              patchState(store, (state) => ({
                {entities}: [...state.{entities}, item],
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      update{Entity}(id: string, request: Update{Entity}Request): void {
        {entity}Api
          .update(id, request)
          .pipe(take(1))
          .subscribe({
            next: (updated) =>
              patchState(store, (state) => ({
                {entities}: replace{Entity}(state.{entities}, id, updated),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },

      delete{Entity}(id: string): void {
        {entity}Api
          .delete(id)
          .pipe(take(1))
          .subscribe({
            next: () =>
              patchState(store, (state) => ({
                {entities}: remove{Entity}(state.{entities}, id),
              })),
            error: (err: unknown) =>
              patchState(store, { error: toErrorMessage(err) }),
          });
      },
    };
  })
);
```

## Rules

- `signalStore` at module level — not inside a class
- `inject()` inside `withMethods` factory function — not at top level
- Always use `take(1)` before `.subscribe()` — never leave open subscriptions
- `patchState` for all state mutations — never mutate state directly
- `toErrorMessage` helper converts `unknown` error to string — always include it
- `replace{Entity}` / `remove{Entity}` — pure helper functions outside the store
- `withComputed` for derived state — use `computed()` from `@angular/core`
- State interface exported as `{Entity}State` so the barrel can re-export it
- No `async`/`await` inside store methods — use `.pipe(take(1)).subscribe()`
- No `firstValueFrom` or `lastValueFrom` — keep the `.pipe(take(1)).subscribe()` pattern
