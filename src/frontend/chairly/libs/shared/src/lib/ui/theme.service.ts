import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly isDark = signal<boolean>(false);

  constructor() {
    const stored = this.document.defaultView?.localStorage.getItem('chairly-theme');
    const isDark = stored === 'dark';
    this.isDark.set(isDark);
    if (isDark) {
      this.enableDarkTheme();
    }
  }

  toggle(): void {
    const newIsDark = !this.isDark();
    this.isDark.set(newIsDark);
    if (newIsDark) {
      this.enableDarkTheme();
    } else {
      this.enableLightTheme();
    }
    this.document.defaultView?.localStorage.setItem('chairly-theme', newIsDark ? 'dark' : 'light');
  }

  private enableDarkTheme(): void {
    this.document.documentElement.setAttribute('data-theme', 'dark');
  }

  private enableLightTheme(): void {
    this.document.documentElement.removeAttribute('data-theme');
  }
}
