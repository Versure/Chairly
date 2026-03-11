import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'chairly-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-header.component.html',
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
}
