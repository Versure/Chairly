import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { FeatureCardComponent, HeroSectionComponent } from '../../ui';

@Component({
  selector: 'chairly-web-landing-page',
  standalone: true,
  imports: [HeroSectionComponent, FeatureCardComponent, RouterLink],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPageComponent {}
