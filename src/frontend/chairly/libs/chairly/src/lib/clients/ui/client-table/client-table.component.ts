import {
  ChangeDetectionStrategy,
  Component,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { ClientResponse } from '../../models';

@Component({
  selector: 'chairly-client-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './client-table.component.html',
})
export class ClientTableComponent {
  readonly clients: InputSignal<ClientResponse[]> = input.required<ClientResponse[]>();

  readonly rowClick: OutputEmitterRef<ClientResponse> = output<ClientResponse>();
  readonly edit: OutputEmitterRef<ClientResponse> = output<ClientResponse>();
  readonly delete: OutputEmitterRef<ClientResponse> = output<ClientResponse>();
}
