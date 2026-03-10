import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { InvoiceStore } from '../../data-access';
import { InvoiceFilterParams, InvoiceStatus, InvoiceSummary } from '../../models';
import { InvoiceStatusBadgePipe } from '../../pipes';

@Component({
  selector: 'chairly-invoice-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    LoadingIndicatorComponent,
    RouterLink,
    InvoiceStatusBadgePipe,
  ],
  templateUrl: './invoice-list-page.component.html',
})
export class InvoiceListPageComponent {
  private readonly invoiceStore = inject(InvoiceStore);
  private readonly router = inject(Router);

  protected readonly invoices = computed<InvoiceSummary[]>(() => this.invoiceStore.invoices());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());

  protected readonly filterClientName = signal<string>('');
  protected readonly filterFromDate = signal<string>('');
  protected readonly filterToDate = signal<string>('');
  protected readonly filterStatus = signal<InvoiceStatus | ''>('');

  protected readonly filters = computed<InvoiceFilterParams>(() => ({
    clientName: this.filterClientName() || undefined,
    fromDate: this.filterFromDate() || undefined,
    toDate: this.filterToDate() || undefined,
    status: this.filterStatus() || undefined,
  }));

  private readonly filterEffect = effect(() => {
    const filters = this.filters();
    this.invoiceStore.loadInvoices(filters);
  });

  protected onRowClick(invoice: InvoiceSummary): void {
    void this.router.navigate(['/facturen', invoice.id]);
  }

  protected onClearFilters(): void {
    this.filterClientName.set('');
    this.filterFromDate.set('');
    this.filterToDate.set('');
    this.filterStatus.set('');
  }
}
