import {
  ChangeDetectionStrategy,
  Component,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
} from '@angular/core';

import { ClientResponse } from '../models';

@Component({
  selector: 'chairly-client-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './client-table.component.html',
})
export class ClientTableComponent {
  readonly clients: InputSignal<ClientResponse[]> = input.required<ClientResponse[]>();

  readonly edit: OutputEmitterRef<ClientResponse> = output<ClientResponse>();
  readonly delete: OutputEmitterRef<ClientResponse> = output<ClientResponse>();
}
