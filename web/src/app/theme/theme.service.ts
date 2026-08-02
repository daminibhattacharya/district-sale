import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'district-sale-theme';

/**
 * Owns the light/dark theme. The choice is reflected onto `<html data-theme>` — which flips the CSS
 * custom properties defined in styles.css — and remembered in localStorage. The first time (no saved
 * choice) it follows the OS preference. Everything is done through a signal, so the toggle stays in
 * sync via change detection and the effect keeps the DOM and storage current.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(this.initial());

  constructor() {
    effect(() => {
      const theme = this.theme();
      document.documentElement.setAttribute('data-theme', theme);
      localStorage.setItem(STORAGE_KEY, theme);
    });
  }

  toggle(): void {
    this.theme.update((t) => (t === 'dark' ? 'light' : 'dark'));
  }

  private initial(): Theme {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === 'light' || saved === 'dark') return saved;
    // matchMedia is absent under jsdom (tests) — fall back to light.
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
