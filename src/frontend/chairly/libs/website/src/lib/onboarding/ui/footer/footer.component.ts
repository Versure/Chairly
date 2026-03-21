import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'chairly-web-footer',
  standalone: true,
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {}
