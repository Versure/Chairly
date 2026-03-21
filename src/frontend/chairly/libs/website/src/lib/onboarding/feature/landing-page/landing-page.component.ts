import { ChangeDetectionStrategy, Component } from '@angular/core';

import {
  FeatureCardComponent,
  FooterComponent,
  HeaderComponent,
  HeroSectionComponent,
} from '../../ui';

@Component({
  selector: 'chairly-web-landing-page',
  standalone: true,
  imports: [HeaderComponent, HeroSectionComponent, FeatureCardComponent, FooterComponent],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingPageComponent {}
