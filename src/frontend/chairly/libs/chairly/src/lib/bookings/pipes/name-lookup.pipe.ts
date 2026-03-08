import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'nameLookup',
  standalone: true,
})
export class NameLookupPipe implements PipeTransform {
  transform(id: string, nameMap: Record<string, string>): string {
    return nameMap[id] ?? id;
  }
}
