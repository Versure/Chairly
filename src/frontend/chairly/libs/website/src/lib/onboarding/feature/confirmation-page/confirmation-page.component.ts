import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { map } from 'rxjs';

@Component({
  selector: 'chairly-web-confirmation-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './confirmation-page.component.html',
  styleUrl: './confirmation-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationPageComponent {
  private readonly route = inject(ActivatedRoute);

  private readonly type = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('type'))),
  );

  protected readonly isSubscription = computed(() => this.type() === 'abonnement');

  protected readonly heading = computed(() => 'Bedankt voor uw aanmelding!');

  protected readonly message = computed(
    () =>
      'Wij verwerken uw aanvraag zo snel mogelijk. U ontvangt een e-mail zodra uw omgeving klaar is.',
  );
}
