import { Pipe, PipeTransform } from '@angular/core';

import { StaffMemberOption } from '../models';

@Pipe({
  name: 'staffColor',
  standalone: true,
})
export class StaffColorPipe implements PipeTransform {
  transform(staffMemberId: string, staffMembers: StaffMemberOption[]): string {
    const member = staffMembers.find((m) => m.id === staffMemberId);
    return member?.color ?? '#6B7280';
  }
}
