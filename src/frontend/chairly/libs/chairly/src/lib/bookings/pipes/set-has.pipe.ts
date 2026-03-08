import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'setHas',
  standalone: true,
})
export class SetHasPipe implements PipeTransform {
  transform(value: string, set: ReadonlySet<string>): boolean {
    return set.has(value);
  }
}
