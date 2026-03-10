import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'chairly-loading-indicator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './loading-indicator.component.html',
})
export class LoadingIndicatorComponent {
  readonly message = input<string>('Laden...');
}
