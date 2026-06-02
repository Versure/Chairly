import { Pipe, PipeTransform } from '@angular/core';

import { NewsletterStatus } from '../models';
import { newsletterStatusLabel } from '../util';

@Pipe({
  name: 'newsletterStatusLabel',
  standalone: true,
})
export class NewsletterStatusLabelPipe implements PipeTransform {
  transform(status: NewsletterStatus | string): string {
    return newsletterStatusLabel(status);
  }
}
