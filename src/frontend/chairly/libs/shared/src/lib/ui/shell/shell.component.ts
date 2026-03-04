import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { ThemeService } from '../theme.service';

@Component({
  selector: 'chairly-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
})
export class ShellComponent {
  protected readonly themeService = inject(ThemeService);
}
