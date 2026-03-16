import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthStore } from '../../data-access';
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
  protected readonly authStore = inject(AuthStore);

  readonly sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  onLogout(): void {
    this.authStore.logout();
  }
}
