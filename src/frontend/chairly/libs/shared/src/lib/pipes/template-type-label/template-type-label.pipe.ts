import { Pipe, PipeTransform } from '@angular/core';

const templateTypeLabels: Record<string, string> = {
  BookingConfirmation: 'Boekingsbevestiging',
  BookingReminder: 'Boekingsherinnering',
  BookingCancellation: 'Boekingsannulering',
  BookingReceived: 'Boeking ontvangen',
  InvoiceSent: 'Factuur verzonden',
};

@Pipe({
  name: 'templateTypeLabel',
  standalone: true,
})
export class TemplateTypeLabelPipe implements PipeTransform {
  transform(type: string): string {
    return templateTypeLabels[type] ?? type;
  }
}
