import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'chairly-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
})
export class ShellComponent {}
