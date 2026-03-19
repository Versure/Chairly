import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import {
  ConfirmationDialogComponent,
  LoadingIndicatorComponent,
  PageHeaderComponent,
} from '@org/shared-lib';

import { ClientApiService, ClientStore } from '../../data-access';
import { ClientResponse, CreateClientRequest } from '../../models';
import { ClientFormDialogComponent, ClientTableComponent } from '../../ui';

@Component({
  selector: 'chairly-client-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ConfirmationDialogComponent,
    PageHeaderComponent,
    ClientFormDialogComponent,
    ClientTableComponent,
    LoadingIndicatorComponent,
  ],
  templateUrl: './client-list-page.component.html',
})
export class ClientListPageComponent implements OnInit {
  private readonly store = inject(ClientStore);
  private readonly clientApi = inject(ClientApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly formDialogRef = viewChild.required(ClientFormDialogComponent);
  private readonly deleteDialogRef =
    viewChild.required<ConfirmationDialogComponent>('deleteDialog');

  protected readonly selectedClient = signal<ClientResponse | null>(null);
  protected readonly clients = this.store.clients;
  protected readonly isLoading = this.store.isLoading;
  protected readonly mutationError = signal<string | null>(null);

  private toMutationErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) {
        return 'Je sessie is verlopen of ongeldig. Log opnieuw in en probeer het nogmaals.';
      }

      if (error.status === 403) {
        return 'Je hebt geen rechten om deze actie uit te voeren.';
      }
    }

    return 'Er is een fout opgetreden. Probeer het opnieuw.';
  }

  ngOnInit(): void {
    this.store.loadAll();
  }

  protected openAddDialog(): void {
    this.selectedClient.set(null);
    this.formDialogRef().open(null);
  }

  protected onEdit(client: ClientResponse): void {
    this.selectedClient.set(client);
    this.formDialogRef().open(client);
  }

  protected onDelete(client: ClientResponse): void {
    this.selectedClient.set(client);
    this.deleteDialogRef().open();
  }

  protected onConfirmDelete(): void {
    const client = this.selectedClient();
    this.mutationError.set(null);
    this.selectedClient.set(null);
    if (client) {
      this.clientApi
        .delete(client.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.store.removeClient(client.id);
          },
          error: (error: unknown) => {
            this.mutationError.set(this.toMutationErrorMessage(error));
          },
        });
    }
  }

  protected onSave(request: CreateClientRequest): void {
    const client = this.selectedClient();
    this.mutationError.set(null);
    this.selectedClient.set(null);
    if (client) {
      this.clientApi
        .update(client.id, request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (updated) => {
            this.store.updateClient(updated);
          },
          error: (error: unknown) => {
            this.mutationError.set(this.toMutationErrorMessage(error));
          },
        });
    } else {
      this.clientApi
        .create(request)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.store.addClient(created);
          },
          error: (error: unknown) => {
            this.mutationError.set(this.toMutationErrorMessage(error));
          },
        });
    }
  }

  protected onRowClick(client: ClientResponse): void {
    this.router.navigate(['/klanten', client.id]);
  }

  protected onCancelled(): void {
    this.selectedClient.set(null);
  }
}
