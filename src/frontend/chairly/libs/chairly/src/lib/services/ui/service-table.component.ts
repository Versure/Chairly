import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output, OutputEmitterRef } from '@angular/core';

import { ServiceResponse } from '../models';
import { DurationPipe } from '../pipes';

@Component({
  selector: 'chairly-service-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DurationPipe],
  templateUrl: './service-table.component.html',
})
export class ServiceTableComponent {
  readonly services = input.required<ServiceResponse[]>();
  readonly isLoading = input.required<boolean>();

  readonly editClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly deleteClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
  readonly toggleActiveClicked: OutputEmitterRef<ServiceResponse> = output<ServiceResponse>();
}
