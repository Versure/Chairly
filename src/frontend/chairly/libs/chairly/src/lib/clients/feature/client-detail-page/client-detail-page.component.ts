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
  BookingStatus,
  BookingTimelineCard,
  ClientRecipeSummary,
  ClientTimeline,
  ClientTimelineEntry,
  ClientTimelineStats,
  CreateClientRequest,
  Recipe,
} from '../../models';
import {
  BookingTimelineCardComponent,
  ClientFormDialogComponent,
  ClientProfileHeaderComponent,
  TimelineStatusFilterComponent,
} from '../../ui';
import { filterByStatus, groupByMonth, MonthGroup } from '../../util';
import { RecipeFormComponent } from '../recipe-form/recipe-form.component';

@Component({
  selector: 'chairly-client-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LoadingIndicatorComponent,
    ClientFormDialogComponent,
    ClientProfileHeaderComponent,
    TimelineStatusFilterComponent,
    BookingTimelineCardComponent,
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

  protected readonly timeline = signal<ClientTimeline | null>(null);
  protected readonly isLoadingTimeline = signal<boolean>(true);
  protected readonly error = signal<string | null>(null);
  protected readonly statusFilter = signal<BookingStatus | 'All'>('All');

  protected readonly selectedRecipeForEdit = signal<Recipe | null>(null);
  protected readonly activeBookingId = signal<string>('');

  protected readonly client = computed(() => this.timeline()?.profile ?? null);
  protected readonly stats = computed<ClientTimelineStats | null>(
    () => this.timeline()?.stats ?? null,
  );
  protected readonly entries = computed<ClientTimelineEntry[]>(
    () => this.timeline()?.timeline ?? [],
  );

  protected readonly filteredEntries = computed<ClientTimelineEntry[]>(() =>
    filterByStatus(this.entries(), this.statusFilter()),
  );

  protected readonly entriesByMonth = computed<MonthGroup<ClientTimelineEntry>[]>(() =>
    groupByMonth(this.filteredEntries()),
  );

  /** Derived from unfiltered entries so auto-open works regardless of active chip. */
  protected readonly completedBookingsWithoutRecipe = computed<ClientTimelineEntry[]>(() =>
    this.entries().filter((e) => e.booking.completedAtUtc !== null && e.recipe === null),
  );

  protected readonly statusCounts = computed<Record<BookingStatus | 'All', number>>(() => {
    const all = this.entries();
    const counts: Record<BookingStatus | 'All', number> = {
      All: all.length,
      Scheduled: 0,
      Confirmed: 0,
      InProgress: 0,
      Completed: 0,
      Cancelled: 0,
      NoShow: 0,
    };
    for (const entry of all) {
      const s = entry.booking.status;
      if (s in counts) {
        counts[s]++;
      }
    }
    return counts;
  });

  private readonly pendingBookingId = signal<string | null>(null);
  private clientId = '';

  ngOnInit(): void {
    this.clientId = this.route.snapshot.paramMap.get('clientId') ?? '';
    const bookingIdParam = this.route.snapshot.queryParamMap.get('bookingId');
    if (bookingIdParam) {
      this.pendingBookingId.set(bookingIdParam);
    }
    this.loadTimeline();
  }

  private loadTimeline(): void {
    this.isLoadingTimeline.set(true);
    this.error.set(null);
    this.clientApi
      .getClientTimeline(this.clientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.timeline.set(data);
          this.isLoadingTimeline.set(false);
          this.tryAutoOpenRecipeForm();
        },
        error: () => {
          this.isLoadingTimeline.set(false);
          this.error.set('Er is een fout opgetreden bij het laden van de klant.');
        },
      });
  }

  private tryAutoOpenRecipeForm(): void {
    const bookingId = this.pendingBookingId();
    if (!bookingId) {
      return;
    }
    const eligibleEntry = this.completedBookingsWithoutRecipe().find(
      (e) => e.booking.id === bookingId,
    );
    if (eligibleEntry) {
      this.pendingBookingId.set(null);
      this.onAddRecipe(eligibleEntry.booking);
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true,
      });
    }
  }

  protected onAddRecipe(booking: BookingTimelineCard): void {
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
    this.selectedRecipeForEdit.set(null);
    this.activeBookingId.set('');
    this.loadTimeline();
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
          const current = this.timeline();
          if (current) {
            this.timeline.set({ ...current, profile: updated });
          }
        },
        error: () => {
          this.error.set('Er is een fout opgetreden bij het bijwerken van de klant.');
        },
      });
  }
}
