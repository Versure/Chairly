import { CurrencyPipe, DatePipe, DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { LoadingIndicatorComponent } from '@org/shared-lib';

import { InvoiceStore } from '../../data-access';
import { AddLineItemRequest, CompanyInfo, Invoice } from '../../models';
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
  protected readonly company = computed<CompanyInfo | null>(() => this.invoiceStore.companyInfo());
  protected readonly isLoading = computed<boolean>(() => this.invoiceStore.isLoading());
  protected readonly error = computed<string | null>(() => this.invoiceStore.error());
  protected readonly actionFeedback = signal<string | null>(null);
  protected readonly actionFeedbackType = signal<'success' | 'error' | null>(null);
  protected readonly isSendingInvoice = signal(false);
  private readonly pendingSendInvoiceId = signal<string | null>(null);

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
    this.invoiceStore.loadCompanyInfo();
  }

  protected onSendInvoice(): void {
    const inv = this.invoice();
    if (inv) {
      this.actionFeedback.set(null);
      this.actionFeedbackType.set(null);
      this.pendingSendInvoiceId.set(inv.id);
      this.isSendingInvoice.set(true);
      this.invoiceStore.sendInvoice(inv.id);
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

  protected onRegenerate(): void {
    const inv = this.invoice();
    if (inv) {
      this.invoiceStore.regenerateInvoice(inv.id);
    }
  }

  protected onPrint(): void {
    this.document.defaultView?.print();
  }

  constructor() {
    effect(() => {
      const pendingId = this.pendingSendInvoiceId();
      if (!pendingId) {
        return;
      }

      const errorMessage = this.error();
      if (errorMessage) {
        this.actionFeedback.set(errorMessage);
        this.actionFeedbackType.set('error');
        this.pendingSendInvoiceId.set(null);
        this.isSendingInvoice.set(false);
        return;
      }

      const inv = this.invoice();
      if (inv?.id === pendingId && inv.status === 'Verzonden') {
        this.actionFeedback.set('Factuur is succesvol verzonden.');
        this.actionFeedbackType.set('success');
        this.pendingSendInvoiceId.set(null);
        this.isSendingInvoice.set(false);
      }
    });
  }
}
