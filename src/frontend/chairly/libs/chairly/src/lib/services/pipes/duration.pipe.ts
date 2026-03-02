import { Pipe, PipeTransform } from '@angular/core';

import { parseDuration } from '../util';

/**
 * Transforms a .NET TimeSpan string ('HH:MM:SS') to a human-readable display.
 * Examples:
 *   '00:30:00' → '30 min'
 *   '01:00:00' → '1h'
 *   '01:30:00' → '1h 30min'
 */
@Pipe({
  name: 'duration',
  standalone: true,
})
export class DurationPipe implements PipeTransform {
  transform(timeSpan: string): string {
    const totalMinutes = parseDuration(timeSpan);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    if (hours === 0) {
      return `${minutes} min`;
    }

    if (minutes === 0) {
      return `${hours}h`;
    }

    return `${hours}h ${minutes}min`;
  }
}
