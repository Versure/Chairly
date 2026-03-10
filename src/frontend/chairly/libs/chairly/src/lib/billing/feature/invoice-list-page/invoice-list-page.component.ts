import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { InvoiceStore } from '../../data-access';
import { InvoiceSummary } from '../../models';
import { InvoiceStatusBadgePipe } from '../../pipes';

@Component({
  selector: 'chairly-invoice-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, LoadingIndicatorComponent, RouterLink, InvoiceStatusBadgePipe],
  templateUrl: './invoice-list-page.component.html',
})
export class InvoiceListPageComponent implements OnInit {
  private readonly invoiceStore = inject(InvoiceStore);

  protected readonly invoices = computed<InvoiceSummary[]>(() => this.invoiceStore.invoices());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());

  ngOnInit(): void {
    this.invoiceStore.loadInvoices();
  }
}
