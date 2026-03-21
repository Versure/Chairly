import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'chairly-web-feature-card',
  standalone: true,
  templateUrl: './feature-card.component.html',
  styleUrl: './feature-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeatureCardComponent {
  readonly title = input<string>();
  readonly description = input<string>();
  readonly iconPath = input<string>();
}
