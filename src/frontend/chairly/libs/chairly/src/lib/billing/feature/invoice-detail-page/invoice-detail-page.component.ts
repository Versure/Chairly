import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { InvoiceStore } from '../../data-access';
import { Invoice } from '../../models';
import { InvoiceStatusBadgePipe } from '../../pipes';

@Component({
  selector: 'chairly-invoice-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, InvoiceStatusBadgePipe],
  templateUrl: './invoice-detail-page.component.html',
})
export class InvoiceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly invoiceStore = inject(InvoiceStore);

  protected readonly invoice = computed<Invoice | null>(() => this.invoiceStore.selectedInvoice());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());

  protected readonly canSend = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && inv.status === 'Concept';
  });

  protected readonly canPay = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && inv.status === 'Verzonden';
  });

  protected readonly canVoid = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && (inv.status === 'Concept' || inv.status === 'Verzonden');
  });

  // eslint-disable-next-line sonarjs/todo-tag -- tracked requirement
  // TODO: Spec says action buttons should be "Owner only" — add role check when auth is implemented
  protected readonly showActions = computed<boolean>(() => {
    return this.canSend() || this.canPay() || this.canVoid();
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.invoiceStore.loadInvoice(id);
    }
  }

  protected onMarkAsSent(): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.markAsSent(inv.id);
    }
  }

  protected onMarkAsPaid(): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.markAsPaid(inv.id);
    }
  }

  protected onVoid(): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.voidInvoice(inv.id);
    }
  }
}
