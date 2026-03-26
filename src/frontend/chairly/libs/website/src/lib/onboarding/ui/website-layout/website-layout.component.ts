import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { FooterComponent } from '../footer/footer.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'chairly-web-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  templateUrl: './website-layout.component.html',
  styleUrl: './website-layout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WebsiteLayoutComponent {}
