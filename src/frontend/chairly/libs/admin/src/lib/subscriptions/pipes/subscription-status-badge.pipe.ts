import { Pipe, PipeTransform } from '@angular/core';

interface StatusBadge {
  label: string;
  cssClass: string;
}

@Pipe({ name: 'subscriptionStatusBadge', standalone: true })
export class SubscriptionStatusBadgePipe implements PipeTransform {
  transform(status: string): StatusBadge {
    switch (status) {
      case 'pending':
        return {
          label: 'In afwachting',
          cssClass: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
        };
      case 'trial':
        return {
          label: 'Proefperiode',
          cssClass: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
        };
      case 'provisioned':
        return {
          label: 'Actief',
          cssClass: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
        };
      case 'cancelled':
        return {
          label: 'Geannuleerd',
          cssClass: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
        };
      default:
        return {
          label: status,
          cssClass: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200',
        };
    }
  }
}
