import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import {
  BookingStatus,
  BookingTimelineCard,
  ClientRecipeSummary,
  ClientTimelineEntry,
  InvoiceStatus,
} from '../../models';
import { BookingStatusLabelPipe } from '../../pipes';

interface StatusBadgeStyle {
  classes: string;
}

const STATUS_BADGE_STYLES: Record<BookingStatus, StatusBadgeStyle> = {
  Scheduled: {
    classes: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200',
  },
  Confirmed: {
    classes: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/40 dark:text-indigo-200',
  },
  InProgress: {
    classes: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200',
  },
  Completed: {
    classes: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  },
  Cancelled: {
    classes: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200',
  },
  NoShow: {
    classes: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
  },
};

const INVOICE_STATUS_LABELS: Record<InvoiceStatus, string> = {
  Draft: 'Concept',
  Sent: 'Verzonden',
  Paid: 'Betaald',
  Void: 'Vervallen',
};

@Component({
  selector: 'chairly-booking-timeline-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, RouterLink, BookingStatusLabelPipe],
  templateUrl: './booking-timeline-card.component.html',
})
export class BookingTimelineCardComponent {
  readonly entry = input.required<ClientTimelineEntry>();
  readonly canEditRecipe = input<boolean>(true);

  readonly addRecipe = output<BookingTimelineCard>();
  readonly editRecipe = output<ClientRecipeSummary>();

  protected readonly showNotes = signal<boolean>(false);

  protected readonly booking = computed(() => this.entry().booking);
  protected readonly recipe = computed(() => this.entry().recipe);
  protected readonly invoice = computed(() => this.entry().invoice);
  protected readonly isCompleted = computed(() => this.booking().completedAtUtc !== null);

  protected readonly statusBadgeClasses = computed(
    () => STATUS_BADGE_STYLES[this.booking().status]?.classes ?? '',
  );

  protected readonly invoiceStatusLabel = computed(() => {
    const inv = this.invoice();
    if (!inv) {
      return '';
    }
    return INVOICE_STATUS_LABELS[inv.status] ?? inv.status;
  });

  protected toggleNotes(): void {
    this.showNotes.set(!this.showNotes());
  }

  protected onAddRecipe(): void {
    this.addRecipe.emit(this.booking());
  }

  protected onEditRecipe(): void {
    const r = this.recipe();
    if (r) {
      this.editRecipe.emit(r);
    }
  }
}
