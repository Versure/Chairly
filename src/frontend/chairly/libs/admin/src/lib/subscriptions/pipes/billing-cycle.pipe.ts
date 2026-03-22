import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'billingCycle', standalone: true })
export class BillingCyclePipe implements PipeTransform {
  transform(value: string | null): string {
    switch (value) {
      case 'Monthly':
        return 'Maandelijks';
      case 'Annual':
        return 'Jaarlijks';
      default:
        return 'N.v.t.';
    }
  }
}
