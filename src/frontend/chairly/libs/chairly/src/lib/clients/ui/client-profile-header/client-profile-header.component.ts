import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ClientResponse, ClientTimelineStats } from '../../models';

@Component({
  selector: 'chairly-client-profile-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './client-profile-header.component.html',
})
export class ClientProfileHeaderComponent {
  readonly client = input.required<ClientResponse>();
  readonly stats = input.required<ClientTimelineStats>();

  readonly editClient = output<void>();

  protected onEdit(): void {
    this.editClient.emit();
  }
}
