import { CurrencyPipe, DatePipe, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { InvoiceStore } from '../../data-access';
import { AddLineItemRequest, Invoice } from '../../models';
import { InvoiceStatusBadgePipe } from '../../pipes';
import { LineItemDialogMode, LineItemFormDialogComponent } from '../../ui';

@Component({
  selector: 'chairly-invoice-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    LineItemFormDialogComponent,
    LoadingIndicatorComponent,
    RouterLink,
    InvoiceStatusBadgePipe,
  ],
  templateUrl: './invoice-detail-page.component.html',
  styleUrl: './invoice-detail-page.component.scss',
})
export class InvoiceDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly invoiceStore = inject(InvoiceStore);
  private readonly document = inject(DOCUMENT);

  private readonly lineItemDialog = viewChild<LineItemFormDialogComponent>('lineItemDialog');

  protected readonly invoice = computed<Invoice | null>(() => this.invoiceStore.selectedInvoice());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());

  protected readonly isDraft = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && inv.status === 'Concept';
  });

  protected readonly isEditable = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && (inv.status === 'Concept' || inv.status === 'Verzonden');
  });

  protected readonly isSent = computed<boolean>(() => {
    const inv = this.invoice();
    return inv !== null && inv.status === 'Verzonden';
  });

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

  protected onOpenLineItemDialog(mode: LineItemDialogMode): void {
    this.lineItemDialog()?.open(mode);
  }

  protected onLineItemSaved(lineItem: AddLineItemRequest): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.addLineItem(inv.id, lineItem);
    }
  }

  protected onRemoveLineItem(lineItemId: string): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.removeLineItem(inv.id, lineItemId);
    }
  }

  protected onPrint(): void {
    this.document.defaultView?.print();
  }
}
