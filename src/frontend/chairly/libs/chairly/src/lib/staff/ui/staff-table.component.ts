import { SlicePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
} from '@angular/core';

import { StaffMemberResponse } from '../models';
import { StaffAvatarComponent } from './staff-avatar.component';

@Component({
  selector: 'chairly-staff-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SlicePipe, StaffAvatarComponent],
  templateUrl: './staff-table.component.html',
})
export class StaffTableComponent {
  readonly staffMembers: InputSignal<StaffMemberResponse[]> = input.required<StaffMemberResponse[]>();

  readonly edit: OutputEmitterRef<StaffMemberResponse> = output<StaffMemberResponse>();
  readonly deactivate: OutputEmitterRef<StaffMemberResponse> = output<StaffMemberResponse>();
  readonly reactivate: OutputEmitterRef<StaffMemberResponse> = output<StaffMemberResponse>();
}
