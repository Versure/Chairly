import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { debounceTime, distinctUntilChanged } from 'rxjs';

import {
  DateInputComponent,
  LoadingIndicatorComponent,
  PageHeaderComponent,
} from '@org/shared-lib';

import { InvoiceStore } from '../../data-access';
import { InvoiceFilterParams, InvoiceStatus, InvoiceSummary } from '../../models';
import { InvoiceStatusBadgePipe } from '../../pipes';

@Component({
  selector: 'chairly-invoice-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DateInputComponent,
    DatePipe,
    FormsModule,
    LoadingIndicatorComponent,
    PageHeaderComponent,
    RouterLink,
    InvoiceStatusBadgePipe,
  ],
  templateUrl: './invoice-list-page.component.html',
})
export class InvoiceListPageComponent {
  private readonly invoiceStore = inject(InvoiceStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly invoices = computed<InvoiceSummary[]>(() => this.invoiceStore.invoices());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());

  protected readonly filterClientName = signal<string>('');
  protected readonly filterFromDate = signal<string>('');
  protected readonly filterToDate = signal<string>('');
  protected readonly filterStatus = signal<InvoiceStatus | ''>('');

  private readonly debouncedClientName = signal<string>('');

  protected readonly filters = computed<InvoiceFilterParams>(() => ({
    clientName: this.debouncedClientName() || undefined,
    fromDate: this.filterFromDate() || undefined,
    toDate: this.filterToDate() || undefined,
    status: this.filterStatus() || undefined,
  }));

  private readonly filterEffect = effect(() => {
    const filters = this.filters();
    this.invoiceStore.loadInvoices(filters);
  });

  constructor() {
    toObservable(this.filterClientName)
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.debouncedClientName.set(value));
  }

  protected onRowClick(invoice: InvoiceSummary): void {
    void this.router.navigate(['/facturen', invoice.id]);
  }

  protected onClearFilters(): void {
    this.filterClientName.set('');
    this.debouncedClientName.set('');
    this.filterFromDate.set('');
    this.filterToDate.set('');
    this.filterStatus.set('');
  }
}
