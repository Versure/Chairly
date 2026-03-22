import { DatePipe, TitleCasePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

import { AdminSubscriptionStore } from '../../data-access';
import { SubscriptionListFilters } from '../../models';
import { SubscriptionStatusBadgePipe } from '../../pipes';

@Component({
  selector: 'chairly-admin-subscription-list-page',
  standalone: true,
  imports: [DatePipe, TitleCasePipe, RouterLink, SubscriptionStatusBadgePipe],
  templateUrl: './subscription-list-page.component.html',
  styleUrl: './subscription-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionListPageComponent implements OnInit {
  private readonly store = inject(AdminSubscriptionStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly searchSubject = new Subject<string>();

  protected readonly items = computed(() => this.store.items());
  protected readonly totalCount = computed(() => this.store.totalCount());
  protected readonly isLoading = computed(() => this.store.isLoading());
  protected readonly page = computed(() => this.store.page());
  protected readonly pageSize = computed(() => this.store.pageSize());
  protected readonly totalPages = computed(() => this.store.totalPages());

  protected readonly searchValue = signal('');
  protected readonly statusFilter = signal('');
  protected readonly planFilter = signal('');

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    const filters: SubscriptionListFilters = {
      search: (params['search'] as string) ?? '',
      status: (params['status'] as string) ?? '',
      plan: (params['plan'] as string) ?? '',
      page: params['page'] ? Number(params['page']) : 1,
      pageSize: params['pageSize'] ? Number(params['pageSize']) : 25,
    };

    this.searchValue.set(filters.search);
    this.statusFilter.set(filters.status);
    this.planFilter.set(filters.plan);

    this.store.loadSubscriptions(filters);

    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.searchValue.set(search);
        this.applyFilters({ page: 1 });
      });
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchSubject.next(value);
  }

  protected onStatusChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value);
    this.applyFilters({ page: 1 });
  }

  protected onPlanChange(event: Event): void {
    this.planFilter.set((event.target as HTMLSelectElement).value);
    this.applyFilters({ page: 1 });
  }

  protected onPageSizeChange(event: Event): void {
    this.applyFilters({ page: 1, pageSize: Number((event.target as HTMLSelectElement).value) });
  }

  protected onPreviousPage(): void {
    const current = this.page();
    if (current > 1) {
      this.applyFilters({ page: current - 1 });
    }
  }

  protected onNextPage(): void {
    const current = this.page();
    if (current < this.totalPages()) {
      this.applyFilters({ page: current + 1 });
    }
  }

  private applyFilters(overrides: Partial<SubscriptionListFilters> = {}): void {
    const filters: SubscriptionListFilters = {
      search: this.searchValue(),
      status: this.statusFilter(),
      plan: this.planFilter(),
      page: overrides.page ?? this.page(),
      pageSize: overrides.pageSize ?? this.pageSize(),
    };

    // Update URL
    const queryParams: Record<string, string | number | undefined> = {};
    if (filters.search) {
      queryParams['search'] = filters.search;
    }
    if (filters.status) {
      queryParams['status'] = filters.status;
    }
    if (filters.plan) {
      queryParams['plan'] = filters.plan;
    }
    if (filters.page > 1) {
      queryParams['page'] = filters.page;
    }
    if (filters.pageSize !== 25) {
      queryParams['pageSize'] = filters.pageSize;
    }

    void this.router.navigate([], {
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });

    this.store.loadSubscriptions(filters);
  }
}
