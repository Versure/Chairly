import { Pipe, PipeTransform } from '@angular/core';

import { NotificationType } from '../models';
import { notificationTypeLabel } from '../util';

@Pipe({
  name: 'notificationTypeLabel',
  standalone: true,
})
export class NotificationTypeLabelPipe implements PipeTransform {
  transform(type: NotificationType): string {
    return notificationTypeLabel(type);
  }
}
