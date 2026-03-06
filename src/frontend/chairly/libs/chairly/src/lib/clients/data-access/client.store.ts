import { inject } from '@angular/core';

import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { take } from 'rxjs';

import { ClientResponse } from '../models';
import { ClientApiService } from './client-api.service';

export interface ClientState {
  clients: ClientResponse[];
  isLoading: boolean;
  error: string | null;
}

const initialState: ClientState = {
  clients: [],
  isLoading: false,
  error: null,
};

function toErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export const ClientStore = signalStore(
  withState<ClientState>(initialState),
  withMethods((store) => {
    const clientApi = inject(ClientApiService);

    return {
      loadAll(): void {
        patchState(store, { isLoading: true, error: null });
        clientApi
          .getAll()
          .pipe(take(1))
          .subscribe({
            next: (clients) => patchState(store, { clients, isLoading: false }),
            error: (err: unknown) =>
              patchState(store, {
                error: toErrorMessage(err),
                isLoading: false,
              }),
          });
      },

      addClient(client: ClientResponse): void {
        patchState(store, (state) => ({
          clients: [...state.clients, client],
        }));
      },

      updateClient(client: ClientResponse): void {
        patchState(store, (state) => ({
          clients: state.clients.map((c) => (c.id === client.id ? client : c)),
        }));
      },

      removeClient(id: string): void {
        patchState(store, (state) => ({
          clients: state.clients.filter((c) => c.id !== id),
        }));
      },
    };
  })
);
