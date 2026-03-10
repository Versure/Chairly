import { Pipe, PipeTransform } from '@angular/core';

import { InvoiceStatus } from '../models';

const statusBadgeClasses: Record<InvoiceStatus, string> = {
  Concept:
    'inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-800 dark:bg-slate-600 dark:text-gray-200',
  Verzonden:
    'inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  Betaald:
    'inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800 dark:bg-green-900 dark:text-green-200',
  Vervallen:
    'inline-flex items-center rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-medium text-red-800 dark:bg-red-900 dark:text-red-200',
};

@Pipe({
  name: 'invoiceStatusBadge',
  standalone: true,
})
export class InvoiceStatusBadgePipe implements PipeTransform {
  transform(status: InvoiceStatus): string {
    return statusBadgeClasses[status] ?? statusBadgeClasses['Concept'];
  }
}
