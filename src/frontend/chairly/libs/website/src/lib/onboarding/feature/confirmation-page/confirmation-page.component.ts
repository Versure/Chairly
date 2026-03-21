import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { map } from 'rxjs';

import { FooterComponent, HeaderComponent } from '../../ui';

@Component({
  selector: 'chairly-web-confirmation-page',
  standalone: true,
  imports: [RouterLink, HeaderComponent, FooterComponent],
  templateUrl: './confirmation-page.component.html',
  styleUrl: './confirmation-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmationPageComponent {
  private readonly route = inject(ActivatedRoute);

  private readonly type = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('type'))),
  );

  protected readonly isDemo = computed(() => this.type() === 'demo');
  protected readonly isSignUp = computed(() => this.type() === 'aanmelding');

  protected readonly heading = computed(() =>
    this.isDemo() ? 'Bedankt voor uw aanvraag!' : 'Bedankt voor uw aanmelding!',
  );

  protected readonly message = computed(() =>
    this.isDemo()
      ? 'Wij hebben uw demo-aanvraag ontvangen en nemen zo snel mogelijk contact met u op.'
      : 'Wij verwerken uw aanvraag zo snel mogelijk. U ontvangt een e-mail zodra uw omgeving klaar is.',
  );
}
