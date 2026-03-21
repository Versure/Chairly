import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'chairly-web-hero-section',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './hero-section.component.html',
  styleUrl: './hero-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeroSectionComponent {
  readonly heading = input.required<string>();
  readonly subheading = input.required<string>();
  readonly primaryCtaLabel = input.required<string>();
  readonly primaryCtaLink = input.required<string>();
  readonly secondaryCtaLabel = input.required<string>();
  readonly secondaryCtaLink = input.required<string>();
}
