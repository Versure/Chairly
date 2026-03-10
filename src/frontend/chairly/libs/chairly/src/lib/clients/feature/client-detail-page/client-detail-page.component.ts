import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { ClientApiService, RecipesApiService } from '../../data-access';
import {
  ClientBookingSummary,
  ClientRecipeSummary,
  ClientResponse,
  CreateClientRequest,
  Recipe,
} from '../../models';
import { ClientFormDialogComponent, ClientRecipeHistoryComponent } from '../../ui';
import { RecipeFormComponent } from '../recipe-form/recipe-form.component';

@Component({
  selector: 'chairly-client-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    LoadingIndicatorComponent,
    ClientFormDialogComponent,
    ClientRecipeHistoryComponent,
    RecipeFormComponent,
  ],
  templateUrl: './client-detail-page.component.html',
})
export class ClientDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clientApi = inject(ClientApiService);
  private readonly recipesApi = inject(RecipesApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly recipeFormRef = viewChild<RecipeFormComponent>('recipeFormRef');
  private readonly clientFormDialogRef = viewChild<ClientFormDialogComponent>('clientFormDialog');

  protected readonly client = signal<ClientResponse | null>(null);
  protected readonly clientRecipes = signal<ClientRecipeSummary[]>([]);
  protected readonly clientBookings = signal<ClientBookingSummary[]>([]);
  protected readonly isLoadingClient = signal<boolean>(true);
  protected readonly isLoadingRecipes = signal<boolean>(true);
  protected readonly isLoadingBookings = signal<boolean>(true);
  protected readonly error = signal<string | null>(null);

  protected readonly clientId = computed<string>(() => {
    const client = this.client();
    return client?.id ?? '';
  });

  protected readonly selectedRecipeForEdit = signal<Recipe | null>(null);
  protected readonly activeBookingId = signal<string>('');

  /** Completed bookings that do not yet have a recipe */
  protected readonly completedBookingsWithoutRecipe = computed<ClientBookingSummary[]>(() => {
    const bookings = this.clientBookings();
    const recipes = this.clientRecipes();
    const recipeBookingIds = new Set(recipes.map((r) => r.bookingId));
    return bookings
      .filter((b) => b.completedAtUtc !== null && !recipeBookingIds.has(b.id))
      .sort((a, b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime());
  });

  private readonly pendingBookingId = signal<string | null>(null);

  ngOnInit(): void {
    const clientId = this.route.snapshot.paramMap.get('clientId') ?? '';
    const bookingIdParam = this.route.snapshot.queryParamMap.get('bookingId');
    if (bookingIdParam) {
      this.pendingBookingId.set(bookingIdParam);
    }
    this.loadClient(clientId);
    this.loadRecipes(clientId);
    this.loadBookings(clientId);
  }

  private loadClient(clientId: string): void {
    this.isLoadingClient.set(true);
    this.clientApi
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (clients) => {
          const found = clients.find((c) => c.id === clientId) ?? null;
          this.client.set(found);
          this.isLoadingClient.set(false);
          if (!found) {
            this.error.set('Klant niet gevonden.');
          }
        },
        error: () => {
          this.isLoadingClient.set(false);
          this.error.set('Er is een fout opgetreden bij het laden van de klant.');
        },
      });
  }

  private loadRecipes(clientId: string): void {
    this.isLoadingRecipes.set(true);
    this.recipesApi
      .getClientRecipes(clientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipes) => {
          this.clientRecipes.set(recipes);
          this.isLoadingRecipes.set(false);
          this.tryAutoOpenRecipeForm();
        },
        error: () => {
          this.isLoadingRecipes.set(false);
        },
      });
  }

  private loadBookings(clientId: string): void {
    this.isLoadingBookings.set(true);
    this.clientApi
      .getClientBookings(clientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (bookings) => {
          this.clientBookings.set(bookings);
          this.isLoadingBookings.set(false);
          this.tryAutoOpenRecipeForm();
        },
        error: () => {
          this.isLoadingBookings.set(false);
        },
      });
  }

  private tryAutoOpenRecipeForm(): void {
    const bookingId = this.pendingBookingId();
    if (!bookingId) {
      return;
    }
    if (this.isLoadingBookings() || this.isLoadingRecipes()) {
      return;
    }
    const eligibleBooking = this.completedBookingsWithoutRecipe().find((b) => b.id === bookingId);
    if (eligibleBooking) {
      this.pendingBookingId.set(null);
      this.onAddRecipe(eligibleBooking);
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true,
      });
    }
  }

  protected onAddRecipe(booking: ClientBookingSummary): void {
    this.selectedRecipeForEdit.set(null);
    this.activeBookingId.set(booking.id);
    this.recipeFormRef()?.open();
  }

  protected onEditRecipe(summary: ClientRecipeSummary): void {
    this.activeBookingId.set(summary.bookingId);
    this.recipesApi
      .getRecipeByBooking(summary.bookingId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          this.selectedRecipeForEdit.set(recipe);
          this.recipeFormRef()?.open(recipe);
        },
        error: () => {
          this.error.set('Er is een fout opgetreden bij het laden van het recept.');
        },
      });
  }

  protected onRecipeSaved(): void {
    const clientId = this.client()?.id;
    if (clientId) {
      this.loadRecipes(clientId);
      this.loadBookings(clientId);
    }
    this.selectedRecipeForEdit.set(null);
    this.activeBookingId.set('');
  }

  protected onRecipeCancelled(): void {
    this.selectedRecipeForEdit.set(null);
    this.activeBookingId.set('');
  }

  protected onEditClient(): void {
    const clientData = this.client();
    if (clientData) {
      this.clientFormDialogRef()?.open(clientData);
    }
  }

  protected onClientSaved(request: CreateClientRequest): void {
    const clientData = this.client();
    if (!clientData) {
      return;
    }
    this.clientApi
      .update(clientData.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.client.set(updated);
        },
        error: () => {
          this.error.set('Er is een fout opgetreden bij het bijwerken van de klant.');
        },
      });
  }
}
